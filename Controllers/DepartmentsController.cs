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
        var departmentCode = request.DepartmentCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Department name is required." });

        if (string.IsNullOrWhiteSpace(departmentCode))
            return BadRequest(new { message = "Department code is required." });

        var exists = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.Name == name);

        if (exists)
            return Conflict(new { message = "Department name already exists." });

        var codeExists = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.DepartmentCode == departmentCode);

        if (codeExists)
            return Conflict(new { message = "Department code already exists." });

        var validationError = await ValidateReferencesAsync(request.HeadOfDepartmentUserId, request.PrimaryBusinessUnitId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var entity = new Department
        {
            DepartmentId = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            Name = name,
            DepartmentCode = departmentCode,
            HeadOfDepartmentUserId = request.HeadOfDepartmentUserId,
            PrimaryBusinessUnitId = request.PrimaryBusinessUnitId,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Departments.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { departmentId = entity.DepartmentId },
            (await BuildResponsesAsync([entity])).Single());
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
            q = q.Where(x => x.Name.Contains(s) || x.DepartmentCode.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var departments = await q.OrderBy(x => x.Name).ToListAsync();
        var rows = await BuildResponsesAsync(departments);
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

        return Ok((await BuildResponsesAsync([entity])).Single());
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
        var departmentCode = request.DepartmentCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Department name is required." });

        if (string.IsNullOrWhiteSpace(departmentCode))
            return BadRequest(new { message = "Department code is required." });

        var dup = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.DepartmentId != departmentId && x.Name == name);

        if (dup)
            return Conflict(new { message = "Department name already exists." });

        var codeDup = await _db.Departments.AsNoTracking()
            .AnyAsync(x => x.DepartmentId != departmentId && x.DepartmentCode == departmentCode);

        if (codeDup)
            return Conflict(new { message = "Department code already exists." });

        var validationError = await ValidateReferencesAsync(request.HeadOfDepartmentUserId, request.PrimaryBusinessUnitId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        entity.Name = name;
        entity.DepartmentCode = departmentCode;
        entity.HeadOfDepartmentUserId = request.HeadOfDepartmentUserId;
        entity.PrimaryBusinessUnitId = request.PrimaryBusinessUnitId;
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
        return Ok((await BuildResponsesAsync([entity])).Single());
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
        x.UnitCode,
        x.HeadOfUnitUserId,
        null,
        0,
        0,
        x.Description,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );

    private async Task<List<DepartmentResponse>> BuildResponsesAsync(IEnumerable<Department> departments)
    {
        var departmentList = departments.ToList();
        var headIds = departmentList
            .Where(x => x.HeadOfDepartmentUserId.HasValue)
            .Select(x => x.HeadOfDepartmentUserId!.Value)
            .Distinct()
            .ToList();

        var businessUnitIds = departmentList
            .Where(x => x.PrimaryBusinessUnitId.HasValue)
            .Select(x => x.PrimaryBusinessUnitId!.Value)
            .Distinct()
            .ToList();

        var departmentIds = departmentList.Select(x => x.DepartmentId).Distinct().ToList();

        var heads = headIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _db.Users.AsNoTracking()
                .Where(x => headIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email);

        var businessUnits = businessUnitIds.Count == 0
            ? new Dictionary<Guid, BusinessUnit>()
            : await _db.BusinessUnits.IgnoreQueryFilters().AsNoTracking()
                .Where(x => businessUnitIds.Contains(x.BusinessUnitId))
                .ToDictionaryAsync(x => x.BusinessUnitId);

        var employeeCounts = await _db.TenantUsers.AsNoTracking()
            .Where(x => x.DepartmentId.HasValue && departmentIds.Contains(x.DepartmentId.Value) && x.IsActive)
            .GroupBy(x => x.DepartmentId!.Value)
            .Select(group => new { DepartmentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);

        return departmentList.Select(department =>
        {
            heads.TryGetValue(department.HeadOfDepartmentUserId ?? Guid.Empty, out var head);
            businessUnits.TryGetValue(department.PrimaryBusinessUnitId ?? Guid.Empty, out var businessUnit);
            employeeCounts.TryGetValue(department.DepartmentId, out var employeeCount);

            return new DepartmentResponse(
                department.DepartmentId,
                department.Name,
                department.DepartmentCode,
                department.HeadOfDepartmentUserId,
                head,
                department.PrimaryBusinessUnitId,
                businessUnit?.Name,
                employeeCount,
                department.Description,
                department.IsActive,
                department.CreatedAtUtc,
                department.UpdatedAtUtc
            );
        }).ToList();
    }

    private async Task<string?> ValidateReferencesAsync(Guid? headOfDepartmentUserId, Guid? primaryBusinessUnitId)
    {
        if (!_tenant.TenantId.HasValue)
            return "Tenant not resolved.";

        var tenantId = _tenant.TenantId.Value;

        if (headOfDepartmentUserId.HasValue)
        {
            var headExists = await _db.TenantUsers.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.UserId == headOfDepartmentUserId.Value && x.IsActive);

            if (!headExists)
                return "Head of department must be an active tenant user.";
        }

        if (primaryBusinessUnitId.HasValue)
        {
            var businessUnitExists = await _db.BusinessUnits.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.BusinessUnitId == primaryBusinessUnitId.Value && x.IsActive);

            if (!businessUnitExists)
                return "Business unit must be an active tenant business unit.";
        }

        return null;
    }
}
