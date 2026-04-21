using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Notifications;
using MultiTenant.Api.Contracts.Tenant;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tenant/users")]
public sealed class TenantUsersController : ControllerBase
{
    private readonly ITenantContext _tenant;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public TenantUsersController(
        ITenantContext tenant,
        UserManager<ApplicationUser> userManager,
        AppDbContext db)
    {
        _tenant = tenant;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TenantUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? businessUnitId = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        IQueryable<TenantUser> q = _db.TenantUsers.AsNoTracking()
            .Where(tu => tu.TenantId == tenantId)
            .Include(tu => tu.Department)
            .Include(tu => tu.BusinessUnit!)
            .ThenInclude(bu => bu.Department);

        if (departmentId.HasValue)
        {
            var scopedBusinessUnitIds = await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == departmentId.Value && d.PrimaryBusinessUnitId.HasValue)
                .Select(d => d.PrimaryBusinessUnitId!.Value)
                .ToListAsync();

            q = q.Where(tu =>
                tu.DepartmentId == departmentId.Value ||
                (tu.BusinessUnit != null && tu.BusinessUnit.DepartmentId == departmentId.Value) ||
                (tu.BusinessUnitId.HasValue && scopedBusinessUnitIds.Contains(tu.BusinessUnitId.Value)));
        }

        if (businessUnitId.HasValue)
            q = q.Where(tu => tu.BusinessUnitId == businessUnitId.Value);

        var memberships = await q.ToListAsync();
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var result = memberships
            .Where(m => users.ContainsKey(m.UserId))
            .OrderBy(m => users[m.UserId].Email)
            .Select(m => ToResponse(m, users[m.UserId], users))
            .ToList();

        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid userId)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var tu = await _db.TenantUsers.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.BusinessUnit!)
            .ThenInclude(bu => bu.Department)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);

        if (tu is null)
            return NotFound();

        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null)
            return NotFound();

        var users = await LoadUsersByIdsAsync(new Guid?[] { userId, tu.LineManagerUserId }.Where(x => x.HasValue).Select(x => x!.Value));
        return Ok(ToResponse(tu, u, users));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateTenantUserRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var org = await ResolveOrgAsync(request.DepartmentId, request.BusinessUnitId);
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

        return Ok(await BuildResponseAsync(user.Id, tenantId));
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid userId, [FromBody] UpdateTenantUserRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var tu = await _db.TenantUsers
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);

        if (tu is null)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
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
            tu.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();

        if (request.EmployeeId != null)
            tu.EmployeeId = string.IsNullOrWhiteSpace(request.EmployeeId) ? null : request.EmployeeId.Trim();

        if (request.LineManagerUserId != tu.LineManagerUserId)
        {
            var managerCheck = await ValidateLineManagerAsync(tenantId, request.LineManagerUserId, userId);
            if (!managerCheck.ok)
                return BadRequest(new { message = managerCheck.error });

            tu.LineManagerUserId = request.LineManagerUserId;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors.Select(e => e.Description));

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!pwdResult.Succeeded)
                return BadRequest(pwdResult.Errors.Select(e => e.Description));
        }

        if (request.IsActive.HasValue)
            tu.IsActive = request.IsActive.Value;

        if (request.UpdateOrganization == true)
        {
            var org = await ResolveOrgAsync(request.DepartmentId, request.BusinessUnitId);
            if (!org.ok)
                return BadRequest(new { message = org.error });

            tu.DepartmentId = org.deptId;
            tu.BusinessUnitId = org.buId;
        }

        await _db.SaveChangesAsync();

        return Ok(await BuildResponseAsync(userId, tenantId));
    }

    [HttpGet("{userId:guid}/preferences")]
    [ProducesResponseType(typeof(TenantUserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences([FromRoute] Guid userId)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var exists = await _db.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId);

        if (!exists)
            return NotFound();

        var settings = await GetOrCreateUserPreferencesAsync(tenantId, userId);
        return Ok(ToPreferencesResponse(settings));
    }

    [HttpPut("{userId:guid}/preferences")]
    [ProducesResponseType(typeof(TenantUserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromRoute] Guid userId, [FromBody] UpdateTenantUserPreferencesRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var exists = await _db.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId);

        if (!exists)
            return NotFound();

        var settings = await GetOrCreateUserPreferencesAsync(tenantId, userId);
        settings.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        settings.PushNotificationsEnabled = request.PushNotificationsEnabled;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToPreferencesResponse(settings));
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid userId)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var tu = await _db.TenantUsers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);
        if (tu is null)
            return NotFound();

        tu.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TenantUserResponse> BuildResponseAsync(Guid userId, Guid tenantId)
    {
        var tu = await _db.TenantUsers.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.BusinessUnit!)
            .ThenInclude(bu => bu.Department)
            .FirstAsync(x => x.TenantId == tenantId && x.UserId == userId);
        var u = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == userId);
        var users = await LoadUsersByIdsAsync(new Guid?[] { userId, tu.LineManagerUserId }.Where(x => x.HasValue).Select(x => x!.Value));
        return ToResponse(tu, u, users);
    }

    internal static TenantUserResponse ToResponse(
        TenantUser m,
        ApplicationUser u,
        IReadOnlyDictionary<Guid, ApplicationUser> users) => new()
    {
        UserId = u.Id,
        Email = u.Email ?? "",
        FullName = u.FullName,
        PhoneNumber = u.PhoneNumber,
        JobTitle = m.JobTitle,
        EmployeeId = m.EmployeeId,
        LineManagerUserId = m.LineManagerUserId,
        LineManagerName = m.LineManagerUserId.HasValue && users.TryGetValue(m.LineManagerUserId.Value, out var lineManager)
            ? lineManager.FullName ?? lineManager.Email
            : null,
        IsActive = m.IsActive,
        DepartmentId = m.DepartmentId ?? m.BusinessUnit?.DepartmentId,
        DepartmentName = m.Department?.Name ?? m.BusinessUnit?.Department?.Name,
        BusinessUnitId = m.BusinessUnitId,
        BusinessUnitName = m.BusinessUnit?.Name
    };

    private async Task<Dictionary<Guid, ApplicationUser>> LoadUsersByIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (!ids.Count.Equals(0))
        {
            return await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        return new Dictionary<Guid, ApplicationUser>();
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

    private async Task<NotificationSetting> GetOrCreateUserPreferencesAsync(Guid tenantId, Guid userId)
    {
        var settings = await _db.NotificationSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);

        if (settings is not null)
            return settings;

        settings = new NotificationSetting
        {
            NotificationSettingId = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EmailExpenseSubmitted = true,
            EmailExpenseApproved = true,
            EmailExpenseRejected = true,
            EmailPendingApprovalsDigest = true,
            EmailNotificationsEnabled = true,
            PushNotificationsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.NotificationSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private static TenantUserPreferencesResponse ToPreferencesResponse(NotificationSetting settings) => new()
    {
        NotificationSettingId = settings.NotificationSettingId,
        EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
        PushNotificationsEnabled = settings.PushNotificationsEnabled,
        CreatedAtUtc = settings.CreatedAtUtc,
        UpdatedAtUtc = settings.UpdatedAtUtc
    };

    private async Task<(bool ok, Guid? deptId, Guid? buId, string? error)> ResolveOrgAsync(
        Guid? departmentId,
        Guid? businessUnitId)
    {
        if (!departmentId.HasValue && !businessUnitId.HasValue)
            return (true, null, null, null);

        if (businessUnitId.HasValue)
        {
            var bu = await _db.BusinessUnits.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId.Value && x.IsActive);

            if (bu is null)
                return (false, null, null, "Business unit not found or inactive.");

            if (departmentId.HasValue)
            {
                var departmentMatches = bu.DepartmentId == departmentId.Value ||
                    await _db.Departments.AsNoTracking()
                        .AnyAsync(x => x.DepartmentId == departmentId.Value && x.PrimaryBusinessUnitId == businessUnitId.Value && x.IsActive);

                if (!departmentMatches)
                    return (false, null, null, "Business unit does not belong to the selected department.");

                return (true, departmentId.Value, businessUnitId, null);
            }

            if (bu.DepartmentId.HasValue)
                return (true, bu.DepartmentId.Value, businessUnitId, null);

            return (false, null, null, "Department is required for the selected business unit.");
        }

        var dept = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId!.Value && x.IsActive);

        if (dept is null)
            return (false, null, null, "Department not found or inactive.");

        return (true, departmentId, null, null);
    }
}
