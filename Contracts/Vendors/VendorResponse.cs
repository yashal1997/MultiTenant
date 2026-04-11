namespace MultiTenant.Api.Contracts.Vendors;

public sealed record VendorResponse(
    Guid VendorId,
    string Code,
    string Name,
    string? LegalName,
    string? Email,
    string? Phone,
    string? Website,
    string? TaxIdentifier,
    string? DefaultCurrency,
    int? PaymentTermsDays,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateRegion,
    string? PostalCode,
    string? Country,
    string? Notes,
    Guid? DefaultGlAccountId,
    string? DefaultGlAccountCode,
    string? DefaultGlAccountName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
