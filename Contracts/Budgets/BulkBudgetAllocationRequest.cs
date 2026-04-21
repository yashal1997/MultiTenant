namespace MultiTenant.Api.Contracts.Budgets;

public sealed class BulkBudgetAllocationRequest
{
    public List<Guid> DepartmentIds { get; set; } = new();
    public List<Guid> ExpenseCategoryIds { get; set; } = new();
    public decimal AllocatedAmount { get; set; }
}
