using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.Budgets;

public sealed class UpdateBudgetRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public string? CurrencyCode { get; set; }
    public BudgetStatus Status { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool IsActive { get; set; }
    public List<BudgetLineInput> Lines { get; set; } = new();
}
