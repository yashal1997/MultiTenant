namespace MultiTenant.Api.Contracts.Budgets;

public sealed class CreateBudgetRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid BusinessUnitId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public List<BudgetLineInput> Lines { get; set; } = new();
}
