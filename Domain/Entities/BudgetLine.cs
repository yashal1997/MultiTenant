namespace MultiTenant.Api.Domain.Entities;

/// <summary>One slice of a <see cref="Budget"/>; at least one scope dimension should be set (dept, BU, category, or GL).</summary>
public sealed class BudgetLine
{
    public Guid BudgetLineId { get; set; }
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = default!;

    /// <summary>Display order (1-based), derived from request list order.</summary>
    public int SequenceOrder { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? BusinessUnitId { get; set; }
    public BusinessUnit? BusinessUnit { get; set; }

    public Guid? ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }

    public Guid? GlAccountId { get; set; }
    public GlAccount? GlAccount { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string? Notes { get; set; }
}
