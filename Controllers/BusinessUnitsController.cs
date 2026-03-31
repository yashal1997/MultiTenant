using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.BusinessUnits;
using MultiTenant.Api.Contracts.Tenant;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/business-units")]
public sealed class BusinessUnitsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public BusinessUnitsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BusinessUnitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessUnitRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var dept = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == request.DepartmentId && x.IsActive);

        if (dept is null)
            return BadRequest(new { message = "Department not found or inactive." });

        var name = request.Name.Trim();
        var exists = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x => x.DepartmentId == request.DepartmentId && x.Name == name);

        if (exists)
            return Conflict(new { message = "Business unit name already exists in this department." });

        var entity = new BusinessUnit
        {
            BusinessUnitId = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            DepartmentId = request.DepartmentId,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessUnits.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { businessUnitId = entity.BusinessUnitId },
            await ToResponseAsync(entity));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BusinessUnitResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.BusinessUnits.AsNoTracking().Include(x => x.Department).AsQueryable();

        if (departmentId.HasValue)
            q = q.Where(x => x.DepartmentId == departmentId.Value);

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var list = await q.OrderBy(x => x.Department!.Name).ThenBy(x => x.Name).ToListAsync();
        var rows = list.Select(x => ToResponse(x, x.Department!)).ToList();
        return Ok(rows);
    }

    [HttpGet("{businessUnitId:guid}")]
    [ProducesResponseType(typeof(BusinessUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid businessUnitId)
    {
        var entity = await _db.BusinessUnits.AsNoTracking()
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId);

        if (entity is null)
            return NotFound();

        return Ok(ToResponse(entity, entity.Department));
    }

    /// <summary>Users assigned to this business unit (business unit contains users).</summary>
    [HttpGet("{businessUnitId:guid}/users")]
    [ProducesResponseType(typeof(List<TenantUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListUsers([FromRoute] Guid businessUnitId)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var buExists = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x => x.BusinessUnitId == businessUnitId);
        if (!buExists)
            return NotFound();

        var memberships = await _db.TenantUsers.AsNoTracking()
            .Where(tu => tu.TenantId == tenantId && tu.BusinessUnitId == businessUnitId)
            .Include(tu => tu.Department)
            .Include(tu => tu.BusinessUnit!)
            .ThenInclude(b => b.Department)
            .ToListAsync();

        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var result = memberships
            .Where(m => users.ContainsKey(m.UserId))
            .OrderBy(m => users[m.UserId].Email)
            .Select(m => TenantUsersController.ToResponse(m, users[m.UserId]))
            .ToList();

        return Ok(result);
    }

    [HttpPut("{businessUnitId:guid}")]
    [ProducesResponseType(typeof(BusinessUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid businessUnitId, [FromBody] UpdateBusinessUnitRequest request)
    {
        var entity = await _db.BusinessUnits
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId);

        if (entity is null)
            return NotFound();

        var name = request.Name.Trim();
        var dup = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x =>
                x.BusinessUnitId != businessUnitId &&
                x.DepartmentId == entity.DepartmentId &&
                x.Name == name);

        if (dup)
            return Conflict(new { message = "Business unit name already exists in this department." });

        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (!entity.IsActive)
            await ClearTenantUserAssignmentsForBusinessUnitAsync(businessUnitId);

        await _db.SaveChangesAsync();
        return Ok(ToResponse(entity, entity.Department));
    }

    [HttpDelete("{businessUnitId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid businessUnitId)
    {
        var entity = await _db.BusinessUnits.FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId);
        if (entity is null)
            return NotFound();

        await ClearTenantUserAssignmentsForBusinessUnitAsync(businessUnitId);

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ClearTenantUserAssignmentsForBusinessUnitAsync(Guid businessUnitId)
    {
        await _db.TenantUsers
            .Where(tu => tu.BusinessUnitId == businessUnitId)
            .ExecuteUpdateAsync(s => s.SetProperty(tu => tu.BusinessUnitId, (Guid?)null));
    }

    private async Task<BusinessUnitResponse> ToResponseAsync(BusinessUnit x)
    {
        var dept = await _db.Departments.AsNoTracking()
            .FirstAsync(d => d.DepartmentId == x.DepartmentId);
        return ToResponse(x, dept);
    }

    private static BusinessUnitResponse ToResponse(BusinessUnit x, Department dept) => new(
        x.BusinessUnitId,
        x.DepartmentId,
        dept.Name,
        x.Name,
        x.Description,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );

}
