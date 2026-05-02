using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiTenant.Api.Contracts.Auth;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;
using MultiTenant.Api.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RegisterRequest = MultiTenant.Api.Contracts.Auth.RegisterRequest;


namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _config = config;
    }

    // 1) REGISTER
    // - Creates Identity user (global)
    // - Creates or attaches tenant
    // - Creates TenantUser membership
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        // Create Identity user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        // Determine tenant
        Tenant tenant;

        if (request.TenantId.HasValue)
        {
            tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == request.TenantId.Value)
                     ?? throw new InvalidOperationException("Tenant not found.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.TenantName))
                return BadRequest("TenantName is required when TenantId is not provided.");

            tenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = request.TenantName.Trim(),
                Status = "ACTIVE",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }

        // Create membership
        var membershipExists = await _db.TenantUsers
            .AnyAsync(x => x.TenantId == tenant.TenantId && x.UserId == user.Id);

        if (!membershipExists)
        {
            _db.TenantUsers.Add(new TenantUser
            {
                TenantUserId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                UserId = user.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // Return list of tenants user belongs to
        var tenants = await GetUserTenants(user.Id);

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Tenants = tenants
        });
    }

    // 2) LOGIN
    // - Validates password
    // - Returns list of tenants user can access
    // - DOES NOT return JWT yet (because user might have multiple tenants)
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var loginId = request.Email.Trim();
        var password = request.Password;

        var normEmail = _userManager.NormalizeEmail(loginId);
        var normUserName = _userManager.NormalizeName(loginId);

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u =>
                (normEmail != null && u.NormalizedEmail == normEmail) ||
                (normUserName != null && u.NormalizedUserName == normUserName));

        if (user is null)
            return Unauthorized("Invalid credentials.");

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            var reason = signIn.IsLockedOut
                ? "User is locked out."
                : signIn.IsNotAllowed
                    ? "User is not allowed to sign in."
                    : "Password validation failed.";

            return Unauthorized(reason);
        }

        var tenants = await GetUserTenants(user.Id);

        var baseToken = CreateBaseJwt(user);

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Tenants = tenants,
            BaseToken = baseToken.AccessToken,
            BaseTokenExpiresUtc = baseToken.ExpiresUtc
        });
    }


    // 3) SELECT TENANT -> issues JWT
    // - Client picks one tenant from login response
    // - Server verifies membership
    // - Issues JWT with tenant_id claim
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [SkipTenantResolution]
    [HttpPost("select-tenant")]
    public async Task<ActionResult<TokenResponse>> SelectTenant([FromBody] SelectTenantRequest request)
    {
        var userId = GetUserIdOrThrow();

        // Ensure tenant is active
        var tenantOk = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.TenantId == request.TenantId && t.Status == "ACTIVE");

        if (!tenantOk)
            return Forbid("Tenant is invalid or inactive.");

        // Verify membership is active
        var isMember = await _db.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == request.TenantId && x.UserId == userId && x.IsActive);

        if (!isMember)
            return Forbid("User is not a member of that tenant.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        var token = CreateJwt(user!, request.TenantId, Array.Empty<string>());
        return Ok(token);
    }


    // ---------------- helpers ----------------
    private TokenResponse CreateBaseJwt(ApplicationUser user)
    {
        var jwt = _config.GetSection("Jwt");
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var key = jwt["Key"]!;

        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new("is_platform_admin", user.IsPlatformAdmin ? "true" : "false")
        // NO tenant_id here
    };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(30);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresUtc = expires
        };
    }

    private Guid GetUserIdOrThrow()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Invalid user id claim.");
        return userId;
    }

    private async Task<List<TenantSummary>> GetUserTenants(Guid userId)
    {
        return await _db.TenantUsers.AsNoTracking()
            .Where(tu => tu.UserId == userId && tu.IsActive)
            .Join(_db.Tenants.AsNoTracking(),
                tu => tu.TenantId,
                t => t.TenantId,
                (tu, t) => new TenantSummary { TenantId = t.TenantId, Name = t.Name })
            .ToListAsync();
    }

    private TokenResponse CreateJwt(ApplicationUser user, Guid tenantId, IEnumerable<string> roles)
    {
        var jwt = _config.GetSection("Jwt");
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var key = jwt["Key"]!;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", tenantId.ToString()),
            new("is_platform_admin", user.IsPlatformAdmin ? "true" : "false")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(6);

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            ExpiresUtc = expires
        };
    }
}
