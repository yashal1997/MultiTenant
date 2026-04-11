using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Vendors;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
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
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        var fieldError = ValidateCommon(code, name, request.PaymentTermsDays, request.DefaultCurrency);
        if (fieldError is not null)
            return BadRequest(new { message = fieldError });

        if (await _db.Vendors.AsNoTracking().AnyAsync(x => x.Code == code))
            return Conflict(new { message = "Vendor code already exists." });

        var glError = await ValidateDefaultGlAsync(tenantId, request.DefaultGlAccountId);
        if (glError is not null)
            return BadRequest(new { message = glError });

        var entity = new Vendor
        {
            VendorId = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            LegalName = request.LegalName?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Website = request.Website?.Trim(),
            TaxIdentifier = request.TaxIdentifier?.Trim(),
            DefaultCurrency = NormalizeCurrency(request.DefaultCurrency),
            PaymentTermsDays = request.PaymentTermsDays,
            AddressLine1 = request.AddressLine1?.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City?.Trim(),
            StateRegion = request.StateRegion?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim(),
            Notes = request.Notes?.Trim(),
            DefaultGlAccountId = request.DefaultGlAccountId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Vendors.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { vendorId = entity.VendorId }, await ToResponseAsync(entity.VendorId));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<VendorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.Vendors.AsNoTracking().Include(x => x.DefaultGlAccount).AsQueryable();

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                x.Name.Contains(s) ||
                (x.LegalName != null && x.LegalName.Contains(s)) ||
                (x.TaxIdentifier != null && x.TaxIdentifier.Contains(s)));
        }

        var rows = await q.OrderBy(x => x.Code).ToListAsync();
        return Ok(rows.Select(ToResponse).ToList());
    }

    [HttpGet("{vendorId:guid}")]
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid vendorId)
    {
        var entity = await _db.Vendors.AsNoTracking()
            .Include(x => x.DefaultGlAccount)
            .FirstOrDefaultAsync(x => x.VendorId == vendorId);

        if (entity is null)
            return NotFound();

        return Ok(ToResponse(entity));
    }

    [HttpPut("{vendorId:guid}")]
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid vendorId, [FromBody] UpdateVendorRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        var fieldError = ValidateCommon(code, name, request.PaymentTermsDays, request.DefaultCurrency);
        if (fieldError is not null)
            return BadRequest(new { message = fieldError });

        var entity = await _db.Vendors.FirstOrDefaultAsync(x => x.VendorId == vendorId);
        if (entity is null)
            return NotFound();

        if (await _db.Vendors.AsNoTracking().AnyAsync(x => x.VendorId != vendorId && x.Code == code))
            return Conflict(new { message = "Vendor code already exists." });

        var glError = await ValidateDefaultGlAsync(tenantId, request.DefaultGlAccountId);
        if (glError is not null)
            return BadRequest(new { message = glError });

        entity.Code = code;
        entity.Name = name;
        entity.LegalName = request.LegalName?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Website = request.Website?.Trim();
        entity.TaxIdentifier = request.TaxIdentifier?.Trim();
        entity.DefaultCurrency = NormalizeCurrency(request.DefaultCurrency);
        entity.PaymentTermsDays = request.PaymentTermsDays;
        entity.AddressLine1 = request.AddressLine1?.Trim();
        entity.AddressLine2 = request.AddressLine2?.Trim();
        entity.City = request.City?.Trim();
        entity.StateRegion = request.StateRegion?.Trim();
        entity.PostalCode = request.PostalCode?.Trim();
        entity.Country = request.Country?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.DefaultGlAccountId = request.DefaultGlAccountId;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(await ToResponseAsync(vendorId));
    }

    [HttpDelete("{vendorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid vendorId)
    {
        var entity = await _db.Vendors.FirstOrDefaultAsync(x => x.VendorId == vendorId);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<VendorResponse> ToResponseAsync(Guid vendorId)
    {
        var entity = await _db.Vendors.AsNoTracking()
            .Include(x => x.DefaultGlAccount)
            .FirstAsync(x => x.VendorId == vendorId);
        return ToResponse(entity);
    }

    private static VendorResponse ToResponse(Vendor x) => new(
        x.VendorId,
        x.Code,
        x.Name,
        x.LegalName,
        x.Email,
        x.Phone,
        x.Website,
        x.TaxIdentifier,
        x.DefaultCurrency,
        x.PaymentTermsDays,
        x.AddressLine1,
        x.AddressLine2,
        x.City,
        x.StateRegion,
        x.PostalCode,
        x.Country,
        x.Notes,
        x.DefaultGlAccountId,
        x.DefaultGlAccount?.Code,
        x.DefaultGlAccount?.Name,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc);

    private static string? ValidateCommon(string code, string name, int? paymentTermsDays, string? defaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Vendor code is required.";
        if (string.IsNullOrWhiteSpace(name))
            return "Vendor name is required.";
        if (paymentTermsDays is < 0)
            return "PaymentTermsDays cannot be negative.";
        if (!string.IsNullOrWhiteSpace(defaultCurrency) && defaultCurrency.Trim().Length != 3)
            return "DefaultCurrency must be a 3-letter ISO 4217 code when provided.";
        return null;
    }

    private static string? NormalizeCurrency(string? c)
    {
        if (string.IsNullOrWhiteSpace(c))
            return null;
        var t = c.Trim().ToUpperInvariant();
        return t.Length == 3 ? t : c.Trim();
    }

    private async Task<string?> ValidateDefaultGlAsync(Guid tenantId, Guid? glAccountId)
    {
        if (!glAccountId.HasValue)
            return null;

        var ok = await _db.GlAccounts.AsNoTracking()
            .AnyAsync(x => x.GlAccountId == glAccountId.Value && x.TenantId == tenantId && x.IsActive);
        return ok ? null : "DefaultGlAccountId is invalid or inactive for this tenant.";
    }
}
