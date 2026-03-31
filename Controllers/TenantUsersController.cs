using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
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
            q = q.Where(tu =>
                tu.DepartmentId == departmentId.Value ||
                (tu.BusinessUnit != null && tu.BusinessUnit.DepartmentId == departmentId.Value));
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
            .Select(m => ToResponse(m, users[m.UserId]))
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

        return Ok(ToResponse(tu, u));
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
        return ToResponse(tu, u);
    }

    internal static TenantUserResponse ToResponse(TenantUser m, ApplicationUser u) => new()
    {
        UserId = u.Id,
        Email = u.Email ?? "",
        FullName = u.FullName,
        PhoneNumber = u.PhoneNumber,
        JobTitle = m.JobTitle,
        IsActive = m.IsActive,
        DepartmentId = m.DepartmentId ?? m.BusinessUnit?.DepartmentId,
        DepartmentName = m.Department?.Name ?? m.BusinessUnit?.Department?.Name,
        BusinessUnitId = m.BusinessUnitId,
        BusinessUnitName = m.BusinessUnit?.Name
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

            if (departmentId.HasValue && departmentId.Value != bu.DepartmentId)
                return (false, null, null, "Business unit does not belong to the selected department.");

            return (true, bu.DepartmentId, businessUnitId, null);
        }

        var dept = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId!.Value && x.IsActive);

        if (dept is null)
            return (false, null, null, "Department not found or inactive.");

        return (true, departmentId, null, null);
    }
}
