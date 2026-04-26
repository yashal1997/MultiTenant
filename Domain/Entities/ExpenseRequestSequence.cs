using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

/// <summary>Per-tenant calendar year counter for <c>REQ-YYYY-####</c> numbers.</summary>
public sealed class ExpenseRequestSequence : ITenantEntity
{
    public Guid ExpenseRequestSequenceId { get; set; }
    public Guid TenantId { get; set; }

    public int Year { get; set; }
    public int LastNumber { get; set; }
}
