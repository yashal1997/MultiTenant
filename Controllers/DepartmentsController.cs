using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.BusinessUnits;
using MultiTenant.Api.Contracts.Departments;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DepartmentsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var name = request.Name.Trim();
        var exists = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.Name == name);

        if (exists)
            return Conflict(new { message = "Department name already exists." });

        var entity = new Department
        {
            DepartmentId = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Departments.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { departmentId = entity.DepartmentId }, ToResponse(entity));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DepartmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.Departments.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var rows = await q.OrderBy(x => x.Name).Select(x => ToResponse(x)).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{departmentId:guid}")]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid departmentId)
    {
        var entity = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId);

        if (entity is null)
            return NotFound();

        return Ok(ToResponse(entity));
    }

    /// <summary>Business units under this department (department contains business-units).</summary>
    [HttpGet("{departmentId:guid}/business-units")]
    [ProducesResponseType(typeof(List<BusinessUnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListBusinessUnits(
        [FromRoute] Guid departmentId,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var dept = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId);
        if (dept is null)
            return NotFound();

        var q = _db.BusinessUnits.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.DepartmentId == departmentId);

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var list = await q.OrderBy(x => x.Name).ToListAsync();
        return Ok(list.Select(x => ToBusinessUnitResponse(x, x.Department!)).ToList());
    }

    [HttpPut("{departmentId:guid}")]
    [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid departmentId, [FromBody] UpdateDepartmentRequest request)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == departmentId);
        if (entity is null)
            return NotFound();

        var name = request.Name.Trim();
        var dup = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.DepartmentId != departmentId && x.Name == name);

        if (dup)
            return Conflict(new { message = "Department name already exists." });

        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (!entity.IsActive)
        {
            await ClearTenantUserAssignmentsForDepartmentAsync(departmentId);
            await _db.BusinessUnits
                .Where(x => x.DepartmentId == departmentId && x.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));
        }

        await _db.SaveChangesAsync();
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid departmentId)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == departmentId);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await ClearTenantUserAssignmentsForDepartmentAsync(departmentId);
        await _db.BusinessUnits
            .Where(x => x.DepartmentId == departmentId && x.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ClearTenantUserAssignmentsForDepartmentAsync(Guid departmentId)
    {
        await _db.TenantUsers
            .Where(tu => tu.DepartmentId == departmentId)
            .ExecuteUpdateAsync(s => s.SetProperty(tu => tu.DepartmentId, (Guid?)null));

        var buIds = await _db.BusinessUnits.AsNoTracking()
            .Where(x => x.DepartmentId == departmentId)
            .Select(x => x.BusinessUnitId)
            .ToListAsync();

        if (buIds.Count == 0)
            return;

        await _db.TenantUsers
            .Where(tu => tu.BusinessUnitId != null && buIds.Contains(tu.BusinessUnitId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(tu => tu.BusinessUnitId, (Guid?)null));
    }

    private static BusinessUnitResponse ToBusinessUnitResponse(BusinessUnit x, Department dept) => new(
        x.BusinessUnitId,
        x.DepartmentId,
        dept.Name,
        x.Name,
        x.Description,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );

    private static DepartmentResponse ToResponse(Department x) => new(
        x.DepartmentId,
        x.Name,
        x.Description,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );
}
