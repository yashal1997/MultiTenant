using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize(Policy = "PlatformAdminOnly")]
[ApiController]
[Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    private readonly AppDbContext _db;
    public PlatformController(AppDbContext db) => _db = db;

    [HttpGet("tenants")]
    public IActionResult GetTenants()
        => Ok(_db.Tenants.ToList());
}