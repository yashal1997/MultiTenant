using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

/// <summary>
/// Tenant-scoped spending plan for a fiscal period. Lines allocate amounts by org and/or category/GL.
/// When <see cref="TotalAmount"/> is set, the sum of line allocations must not exceed it.
/// </summary>
public sealed class Budget : ITenantEntity
{
    public Guid BudgetId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public Guid BusinessUnitId { get; set; }
    public BusinessUnit BusinessUnit { get; set; } = default!;

    /// <summary>Fiscal year label (e.g. 2026). Used with <see cref="Name"/> for uniqueness.</summary>
    public int FiscalYear { get; set; }

    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }

    /// <summary>ISO 4217 (e.g. USD).</summary>
    public string CurrencyCode { get; set; } = "USD";

    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    /// <summary>Optional cap; when set, sum of <see cref="Lines"/> <c>AllocatedAmount</c> must be &lt;= this value.</summary>
    public decimal? TotalAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}
