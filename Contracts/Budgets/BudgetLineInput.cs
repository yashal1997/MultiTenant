namespace MultiTenant.Api.Contracts.Budgets;

public sealed class BudgetLineInput
{
    public Guid? DepartmentId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public Guid? GlAccountId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string? Notes { get; set; }
}
