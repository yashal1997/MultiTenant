using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

/// <summary>Tenant-scoped supplier / payee master data.</summary>
public sealed class Vendor : ITenantEntity
{
    public Guid VendorId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Unique vendor code within the tenant (e.g. ERP vendor number).</summary>
    public string Code { get; set; } = default!;

    /// <summary>Display / trade name.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Legal entity name when different from <see cref="Name"/>.</summary>
    public string? LegalName { get; set; }

    /// <summary>Client-facing vendor category label.</summary>
    public string? Category { get; set; }

    /// <summary>Primary descriptive summary used in the vendor screens.</summary>
    public string? Description { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }

    /// <summary>Tax ID, VAT number, or similar.</summary>
    public string? TaxIdentifier { get; set; }

    /// <summary>ISO 4217 currency code (e.g. USD).</summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>Net payment terms in days (e.g. 30 for Net 30).</summary>
    public int? PaymentTermsDays { get; set; }

    /// <summary>Preferred payment method label shown in the client UI.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Whether tax applies to this vendor by default.</summary>
    public bool IsTaxApplicable { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateRegion { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string? Notes { get; set; }

    /// <summary>Optional default GL for AP / expense coding for this vendor.</summary>
    public Guid? DefaultGlAccountId { get; set; }
    public GlAccount? DefaultGlAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
