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

        var name = request.Name.Trim();
        var unitCode = request.UnitCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Business unit name is required." });

        if (string.IsNullOrWhiteSpace(unitCode))
            return BadRequest(new { message = "Business unit code is required." });

        var validationError = await ValidateReferencesAsync(request.DepartmentId, request.HeadOfUnitUserId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var exists = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x => x.TenantId == _tenant.TenantId.Value && x.Name == name);

        if (exists)
            return Conflict(new { message = "Business unit name already exists." });

        var codeExists = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x => x.TenantId == _tenant.TenantId.Value && x.UnitCode == unitCode);

        if (codeExists)
            return Conflict(new { message = "Business unit code already exists." });

        var entity = new BusinessUnit
        {
            BusinessUnitId = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            DepartmentId = request.DepartmentId,
            HeadOfUnitUserId = request.HeadOfUnitUserId,
            Name = name,
            UnitCode = unitCode,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessUnits.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { businessUnitId = entity.BusinessUnitId },
            (await BuildResponsesAsync([entity])).Single());
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
            q = q.Where(x =>
                x.DepartmentId == departmentId.Value ||
                _db.Departments.Any(d => d.DepartmentId == departmentId.Value && d.PrimaryBusinessUnitId == x.BusinessUnitId));

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || x.UnitCode.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var list = await q.OrderBy(x => x.Name).ToListAsync();
        var rows = await BuildResponsesAsync(list);
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

        return Ok((await BuildResponsesAsync([entity])).Single());
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
            .Select(m => TenantUsersController.ToResponse(m, users[m.UserId], users))
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
        var unitCode = request.UnitCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Business unit name is required." });

        if (string.IsNullOrWhiteSpace(unitCode))
            return BadRequest(new { message = "Business unit code is required." });

        var validationError = await ValidateReferencesAsync(request.DepartmentId, request.HeadOfUnitUserId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var dup = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x =>
                x.BusinessUnitId != businessUnitId &&
                x.TenantId == entity.TenantId &&
                x.Name == name);

        if (dup)
            return Conflict(new { message = "Business unit name already exists." });

        var codeDup = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x =>
                x.BusinessUnitId != businessUnitId &&
                x.TenantId == entity.TenantId &&
                x.UnitCode == unitCode);

        if (codeDup)
            return Conflict(new { message = "Business unit code already exists." });

        entity.DepartmentId = request.DepartmentId;
        entity.HeadOfUnitUserId = request.HeadOfUnitUserId;
        entity.Name = name;
        entity.UnitCode = unitCode;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (!entity.IsActive)
            await ClearTenantUserAssignmentsForBusinessUnitAsync(businessUnitId);

        await _db.SaveChangesAsync();
        return Ok((await BuildResponsesAsync([entity])).Single());
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

    private async Task<string?> ValidateReferencesAsync(Guid? departmentId, Guid? headOfUnitUserId)
    {
        if (!_tenant.TenantId.HasValue)
            return "Tenant not resolved.";

        var tenantId = _tenant.TenantId.Value;

        if (departmentId.HasValue)
        {
            var departmentExists = await _db.Departments.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.DepartmentId == departmentId.Value && x.IsActive);

            if (!departmentExists)
                return "Department not found or inactive.";
        }

        if (headOfUnitUserId.HasValue)
        {
            var headExists = await _db.TenantUsers.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.UserId == headOfUnitUserId.Value && x.IsActive);

            if (!headExists)
                return "Head of unit must be an active tenant user.";
        }

        return null;
    }

    private async Task<List<BusinessUnitResponse>> BuildResponsesAsync(IEnumerable<BusinessUnit> businessUnits)
    {
        var businessUnitList = businessUnits.ToList();
        var businessUnitIds = businessUnitList.Select(x => x.BusinessUnitId).Distinct().ToList();
        var departmentIds = businessUnitList.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value).Distinct().ToList();
        var headIds = businessUnitList.Where(x => x.HeadOfUnitUserId.HasValue).Select(x => x.HeadOfUnitUserId!.Value).Distinct().ToList();

        var departments = departmentIds.Count == 0
            ? new Dictionary<Guid, Department>()
            : await _db.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(x => departmentIds.Contains(x.DepartmentId))
                .ToDictionaryAsync(x => x.DepartmentId);

        var heads = headIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _db.Users.AsNoTracking()
                .Where(x => headIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email);

        var departmentCounts = await _db.Departments.AsNoTracking()
            .Where(x => x.PrimaryBusinessUnitId.HasValue && businessUnitIds.Contains(x.PrimaryBusinessUnitId.Value) && x.IsActive)
            .GroupBy(x => x.PrimaryBusinessUnitId!.Value)
            .Select(group => new { BusinessUnitId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.BusinessUnitId, x => x.Count);

        var employeeCounts = await _db.TenantUsers.AsNoTracking()
            .Where(x => x.BusinessUnitId.HasValue && businessUnitIds.Contains(x.BusinessUnitId.Value) && x.IsActive)
            .GroupBy(x => x.BusinessUnitId!.Value)
            .Select(group => new { BusinessUnitId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.BusinessUnitId, x => x.Count);

        return businessUnitList.Select(unit =>
        {
            Department? department = null;
            if (unit.DepartmentId.HasValue)
                departments.TryGetValue(unit.DepartmentId.Value, out department);

            heads.TryGetValue(unit.HeadOfUnitUserId ?? Guid.Empty, out var headName);
            departmentCounts.TryGetValue(unit.BusinessUnitId, out var departmentCount);
            employeeCounts.TryGetValue(unit.BusinessUnitId, out var employeeCount);

            return new BusinessUnitResponse(
                unit.BusinessUnitId,
                unit.DepartmentId,
                department?.Name,
                unit.Name,
                unit.UnitCode,
                unit.HeadOfUnitUserId,
                headName,
                departmentCount,
                employeeCount,
                unit.Description,
                unit.IsActive,
                unit.CreatedAtUtc,
                unit.UpdatedAtUtc
            );
        }).ToList();
    }
}
