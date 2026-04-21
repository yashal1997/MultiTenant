using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Settings;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public SettingsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("general")]
    [ProducesResponseType(typeof(GeneralSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGeneral()
    {
        if (!_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant not resolved." });

        var settings = await GetOrCreateGeneralSettingsAsync(_tenant.TenantId.Value);
        return Ok(ToResponse(settings));
    }

    [HttpPut("general")]
    [ProducesResponseType(typeof(GeneralSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGeneral([FromBody] UpdateGeneralSettingsRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant not resolved." });

        var companyName = request.CompanyName.Trim();
        var countryCode = request.CountryCode.Trim().ToUpperInvariant();
        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        var timeZoneId = request.TimeZoneId.Trim();
        var dateFormat = request.DateFormat.Trim();

        if (string.IsNullOrWhiteSpace(companyName))
            return BadRequest(new { message = "CompanyName is required." });

        if (countryCode.Length != 2)
            return BadRequest(new { message = "CountryCode must be a 2-letter code." });

        if (currencyCode.Length != 3)
            return BadRequest(new { message = "CurrencyCode must be a 3-letter code." });

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return BadRequest(new { message = "TimeZoneId is required." });

        if (string.IsNullOrWhiteSpace(dateFormat))
            return BadRequest(new { message = "DateFormat is required." });

        if (request.FiscalYearStartMonth is < 1 or > 12)
            return BadRequest(new { message = "FiscalYearStartMonth must be between 1 and 12." });

        var tenantId = _tenant.TenantId.Value;
        var settings = await GetOrCreateGeneralSettingsAsync(tenantId);

        settings.CompanyName = companyName;
        settings.LegalName = Clean(request.LegalName);
        settings.SupportEmail = Clean(request.SupportEmail);
        settings.PhoneNumber = Clean(request.PhoneNumber);
        settings.WebsiteUrl = Clean(request.WebsiteUrl);
        settings.TaxRegistrationNumber = Clean(request.TaxRegistrationNumber);
        settings.AddressLine1 = Clean(request.AddressLine1);
        settings.AddressLine2 = Clean(request.AddressLine2);
        settings.City = Clean(request.City);
        settings.StateOrProvince = Clean(request.StateOrProvince);
        settings.PostalCode = Clean(request.PostalCode);
        settings.CountryCode = countryCode;
        settings.CurrencyCode = currencyCode;
        settings.TimeZoneId = timeZoneId;
        settings.DateFormat = dateFormat;
        settings.FiscalYearStartMonth = request.FiscalYearStartMonth;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(settings));
    }

    private async Task<TenantGeneralSetting> GetOrCreateGeneralSettingsAsync(Guid tenantId)
    {
        var settings = await _db.TenantGeneralSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        if (settings is not null)
            return settings;

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        var fallbackName = tenant?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(fallbackName))
            fallbackName = "My Organization";

        settings = new TenantGeneralSetting
        {
            TenantGeneralSettingId = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyName = fallbackName,
            CountryCode = "US",
            CurrencyCode = "USD",
            TimeZoneId = "UTC",
            DateFormat = "yyyy-MM-dd",
            FiscalYearStartMonth = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.TenantGeneralSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    private static GeneralSettingsResponse ToResponse(TenantGeneralSetting x) => new(
        x.TenantGeneralSettingId,
        x.TenantId,
        x.CompanyName,
        x.LegalName,
        x.SupportEmail,
        x.PhoneNumber,
        x.WebsiteUrl,
        x.TaxRegistrationNumber,
        x.AddressLine1,
        x.AddressLine2,
        x.City,
        x.StateOrProvince,
        x.PostalCode,
        x.CountryCode,
        x.CurrencyCode,
        x.TimeZoneId,
        x.DateFormat,
        x.FiscalYearStartMonth,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );
}
