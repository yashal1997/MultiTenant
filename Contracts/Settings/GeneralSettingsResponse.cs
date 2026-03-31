namespace MultiTenant.Api.Contracts.Settings;

public sealed record GeneralSettingsResponse(
    Guid TenantGeneralSettingId,
    Guid TenantId,
    string CompanyName,
    string? LegalName,
    string? SupportEmail,
    string? PhoneNumber,
    string? WebsiteUrl,
    string? TaxRegistrationNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string CountryCode,
    string CurrencyCode,
    string TimeZoneId,
    string DateFormat,
    int FiscalYearStartMonth,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
