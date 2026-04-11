namespace MultiTenant.Api.Contracts.Vendors;

public sealed class CreateVendorRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? TaxIdentifier { get; set; }
    public string? DefaultCurrency { get; set; }
    public int? PaymentTermsDays { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateRegion { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }
    /// <summary>Optional GL account id for default expense/AP coding.</summary>
    public Guid? DefaultGlAccountId { get; set; }
}
