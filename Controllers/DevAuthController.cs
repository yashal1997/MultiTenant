using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/dev")]
public class DevAuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public DevAuthController(IConfiguration config) => _config = config;

    // Example:
    // GET /api/dev/token?tenantId=...&userId=...&role=FinanceAdmin
    [AllowAnonymous]
    [HttpGet("token")]
    public IActionResult Token([FromQuery] Guid tenantId, [FromQuery] Guid? userId = null, [FromQuery] string? role = null)
    {
        if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true)
            return Forbid("Dev token endpoint is only allowed in Development.");

        var jwt = _config.GetSection("Jwt");
        var key = jwt["Key"]!;
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;

        var uid = userId ?? Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, uid.ToString()),
            new Claim("tenant_id", tenantId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return Ok(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            tenant_id = tenantId,
            user_id = uid,
            expires_utc = token.ValidTo
        });
    }
}
