using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.Budgets;

public sealed class CreateBudgetRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public string? CurrencyCode { get; set; }
    /// <summary>Defaults to <see cref="BudgetStatus.Draft"/>.</summary>
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;
    public decimal? TotalAmount { get; set; }
    public List<BudgetLineInput> Lines { get; set; } = new();
}
