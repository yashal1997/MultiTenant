using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Contracts.Platform;
using MultiTenant.Api.Contracts.Tenant;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize(Policy = "PlatformAdminOnly")]
[ApiController]
[Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
        => Ok(await _db.Tenants.AsNoTracking().OrderBy(x => x.Name).ToListAsync());

    [HttpGet("tenants/{tenantId:guid}")]
    public async Task<IActionResult> GetTenant([FromRoute] Guid tenantId)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Tenant name is required.");

        var normalizedName = request.Name.Trim();
        var exists = await _db.Tenants.AnyAsync(x => x.Name == normalizedName);
        if (exists)
            return Conflict("A tenant with this name already exists.");

        var tenant = new Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = normalizedName,
            Status = "ACTIVE",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        return Ok(tenant);
    }

    [HttpPut("tenants/{tenantId:guid}")]
    public async Task<IActionResult> UpdateTenant([FromRoute] Guid tenantId, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenant is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            var exists = await _db.Tenants.AnyAsync(x => x.TenantId != tenantId && x.Name == normalizedName);
            if (exists)
                return Conflict("A tenant with this name already exists.");

            tenant.Name = normalizedName;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var normalizedStatus = request.Status.Trim().ToUpperInvariant();
            if (normalizedStatus is not ("ACTIVE" or "SUSPENDED"))
                return BadRequest("Status must be ACTIVE or SUSPENDED.");

            tenant.Status = normalizedStatus;
        }

        await _db.SaveChangesAsync();
        return Ok(tenant);
    }

    [HttpDelete("tenants/{tenantId:guid}")]
    public async Task<IActionResult> DeleteTenant([FromRoute] Guid tenantId)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenant is null)
            return NotFound();

        tenant.Status = "SUSPENDED";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("tenants/{tenantId:guid}/users")]
    [ProducesResponseType(typeof(List<TenantUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantUsers([FromRoute] Guid tenantId)
    {
        if (!await _db.Tenants.AsNoTracking().AnyAsync(x => x.TenantId == tenantId))
            return NotFound();

        var memberships = await _db.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        var users = await LoadUsersAsync(memberships);
        var departments = await LoadDepartmentsAsync(memberships);
        var businessUnits = await LoadBusinessUnitsAsync(memberships);

        var result = memberships
            .Where(x => users.ContainsKey(x.UserId))
            .Select(x => ToResponse(x, users[x.UserId], users, departments, businessUnits))
            .OrderBy(x => x.Email)
            .ToList();

        return Ok(result);
    }

    [HttpPost("tenants/{tenantId:guid}/users")]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTenantUser([FromRoute] Guid tenantId, [FromBody] CreateTenantUserRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenant is null)
            return NotFound();

        var org = await ResolveOrgAsync(tenantId, request.DepartmentId, request.BusinessUnitId);
        if (!org.ok)
            return BadRequest(new { message = org.error });

        var managerCheck = await ValidateLineManagerAsync(tenantId, request.LineManagerUserId, null);
        if (!managerCheck.ok)
            return BadRequest(new { message = managerCheck.error });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsPlatformAdmin = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        _db.TenantUsers.Add(new TenantUser
        {
            TenantUserId = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            IsActive = true,
            DepartmentId = org.deptId,
            BusinessUnitId = org.buId,
            JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
            EmployeeId = string.IsNullOrWhiteSpace(request.EmployeeId) ? null : request.EmployeeId.Trim(),
            LineManagerUserId = request.LineManagerUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(await BuildTenantUserResponseAsync(tenantId, user.Id));
    }

    [HttpPut("tenants/{tenantId:guid}/users/{userId:guid}")]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTenantUser([FromRoute] Guid tenantId, [FromRoute] Guid userId, [FromBody] UpdateTenantUserRequest request)
    {
        var membership = await _db.TenantUsers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);
        if (membership is null)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
            user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
            user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
        }

        if (request.FullName != null)
            user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();

        if (request.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.JobTitle != null)
            membership.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();

        if (request.EmployeeId != null)
            membership.EmployeeId = string.IsNullOrWhiteSpace(request.EmployeeId) ? null : request.EmployeeId.Trim();

        if (request.LineManagerUserId != membership.LineManagerUserId)
        {
            var managerCheck = await ValidateLineManagerAsync(tenantId, request.LineManagerUserId, userId);
            if (!managerCheck.ok)
                return BadRequest(new { message = managerCheck.error });

            membership.LineManagerUserId = request.LineManagerUserId;
        }

        if (request.UpdateOrganization == true)
        {
            var org = await ResolveOrgAsync(tenantId, request.DepartmentId, request.BusinessUnitId);
            if (!org.ok)
                return BadRequest(new { message = org.error });

            membership.DepartmentId = org.deptId;
            membership.BusinessUnitId = org.buId;
        }

        if (request.IsActive.HasValue)
            membership.IsActive = request.IsActive.Value;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors.Select(e => e.Description));

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!resetResult.Succeeded)
                return BadRequest(resetResult.Errors.Select(e => e.Description));
        }

        await _db.SaveChangesAsync();
        return Ok(await BuildTenantUserResponseAsync(tenantId, userId));
    }

    [HttpDelete("tenants/{tenantId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> DeleteTenantUser([FromRoute] Guid tenantId, [FromRoute] Guid userId)
    {
        var membership = await _db.TenantUsers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);
        if (membership is null)
            return NotFound();

        membership.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(List<TenantUser> memberships)
    {
        var userIds = memberships.Select(x => x.UserId).Distinct().ToList();
        return await _db.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
    }

    private async Task<Dictionary<Guid, ApplicationUser>> LoadUsersByIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, ApplicationUser>();

        return await _db.Users.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
    }

    private async Task<Dictionary<Guid, Department>> LoadDepartmentsAsync(List<TenantUser> memberships)
    {
        var departmentIds = memberships.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value).Distinct().ToList();
        return await _db.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(x => departmentIds.Contains(x.DepartmentId))
            .ToDictionaryAsync(x => x.DepartmentId);
    }

    private async Task<Dictionary<Guid, BusinessUnit>> LoadBusinessUnitsAsync(List<TenantUser> memberships)
    {
        var businessUnitIds = memberships.Where(x => x.BusinessUnitId.HasValue).Select(x => x.BusinessUnitId!.Value).Distinct().ToList();
        return await _db.BusinessUnits.IgnoreQueryFilters().AsNoTracking()
            .Where(x => businessUnitIds.Contains(x.BusinessUnitId))
            .ToDictionaryAsync(x => x.BusinessUnitId);
    }

    private async Task<TenantUserResponse> BuildTenantUserResponseAsync(Guid tenantId, Guid userId)
    {
        var membership = await _db.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .FirstAsync();

        var user = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == userId);
        var departments = await LoadDepartmentsAsync([membership]);
        var businessUnits = await LoadBusinessUnitsAsync([membership]);

        var users = await LoadUsersByIdsAsync(new Guid?[] { membership.UserId, membership.LineManagerUserId }.Where(x => x.HasValue).Select(x => x!.Value));
        return ToResponse(membership, user, users, departments, businessUnits);
    }

    private static TenantUserResponse ToResponse(
        TenantUser membership,
        ApplicationUser user,
        IReadOnlyDictionary<Guid, ApplicationUser> users,
        IReadOnlyDictionary<Guid, Department> departments,
        IReadOnlyDictionary<Guid, BusinessUnit> businessUnits)
    {
        Department? department = null;
        if (membership.DepartmentId.HasValue)
            departments.TryGetValue(membership.DepartmentId.Value, out department);

        BusinessUnit? businessUnit = null;
        if (membership.BusinessUnitId.HasValue)
            businessUnits.TryGetValue(membership.BusinessUnitId.Value, out businessUnit);

        if (department is null && businessUnit is not null && businessUnit.DepartmentId.HasValue)
            departments.TryGetValue(businessUnit.DepartmentId.Value, out department);

        return new TenantUserResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            JobTitle = membership.JobTitle,
            EmployeeId = membership.EmployeeId,
            LineManagerUserId = membership.LineManagerUserId,
            LineManagerName = membership.LineManagerUserId.HasValue && users.TryGetValue(membership.LineManagerUserId.Value, out var lineManager)
                ? lineManager.FullName ?? lineManager.Email
                : null,
            IsActive = membership.IsActive,
            DepartmentId = department?.DepartmentId,
            DepartmentName = department?.Name,
            BusinessUnitId = businessUnit?.BusinessUnitId,
            BusinessUnitName = businessUnit?.Name
        };
    }

    private async Task<(bool ok, Guid? deptId, Guid? buId, string? error)> ResolveOrgAsync(Guid tenantId, Guid? departmentId, Guid? businessUnitId)
    {
        if (!departmentId.HasValue && !businessUnitId.HasValue)
            return (true, null, null, null);

        if (businessUnitId.HasValue)
        {
            var bu = await _db.BusinessUnits.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId.Value && x.TenantId == tenantId && x.IsActive);

            if (bu is null)
                return (false, null, null, "Business unit not found or inactive.");

            if (departmentId.HasValue)
            {
                var departmentMatches = bu.DepartmentId == departmentId.Value ||
                    await _db.Departments.IgnoreQueryFilters().AsNoTracking()
                        .AnyAsync(x => x.DepartmentId == departmentId.Value && x.TenantId == tenantId && x.PrimaryBusinessUnitId == businessUnitId.Value && x.IsActive);

                if (!departmentMatches)
                    return (false, null, null, "Business unit does not belong to the selected department.");

                return (true, departmentId.Value, businessUnitId, null);
            }

            if (bu.DepartmentId.HasValue)
                return (true, bu.DepartmentId.Value, businessUnitId, null);

            return (false, null, null, "Department is required for the selected business unit.");
        }

        var department = await _db.Departments.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId!.Value && x.TenantId == tenantId && x.IsActive);

        if (department is null)
            return (false, null, null, "Department not found or inactive.");

        return (true, departmentId, null, null);
    }

    private async Task<(bool ok, string? error)> ValidateLineManagerAsync(Guid tenantId, Guid? lineManagerUserId, Guid? targetUserId)
    {
        if (!lineManagerUserId.HasValue)
            return (true, null);

        if (targetUserId.HasValue && lineManagerUserId.Value == targetUserId.Value)
            return (false, "User cannot be their own line manager.");

        var managerExists = await _db.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == lineManagerUserId.Value && x.IsActive);

        return managerExists
            ? (true, null)
            : (false, "Line manager must be an active member of this tenant.");
    }
}
