using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class TenantGeneralSetting : ITenantEntity
{
    public Guid TenantGeneralSettingId { get; set; }
    public Guid TenantId { get; set; }

    public string CompanyName { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? SupportEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TaxRegistrationNumber { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "US";

    public string CurrencyCode { get; set; } = "USD";
    public string TimeZoneId { get; set; } = "UTC";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public int FiscalYearStartMonth { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
