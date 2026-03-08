using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public sealed class VendorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public VendorsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name)
    {
        var vendor = new Vendor
        {
            VendorId = Guid.NewGuid(),
            TenantId = _tenant.TenantId!.Value,
            Name = name
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        return Ok(vendor);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var vendors = await _db.Vendors.AsNoTracking().ToListAsync();
        return Ok(vendors);
    }
}
