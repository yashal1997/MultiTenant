using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Tenant;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize] // requires tenant token for normal users
[ApiController]
[Route("api/tenant/users")]
public sealed class TenantUsersController : ControllerBase
{
    private readonly ITenantContext _tenant;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public TenantUsersController(ITenantContext tenant,
        UserManager<ApplicationUser> userManager,
        AppDbContext db)
    {
        _tenant = tenant;
        _userManager = userManager;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantUserRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        // create global Identity user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsPlatformAdmin = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // bind to SAME tenant as admin’s token
        _db.TenantUsers.Add(new TenantUser
        {
            TenantUserId = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, TenantId = tenantId });
    }
}