namespace MultiTenant.Api.Contracts.Budgets;

public sealed class AdjustBudgetRequest
{
    public Guid FromDepartmentId { get; set; }
    public Guid? ToDepartmentId { get; set; }
    public Guid FromExpenseCategoryId { get; set; }
    public Guid? ToExpenseCategoryId { get; set; }
    public decimal Amount { get; set; }
}
