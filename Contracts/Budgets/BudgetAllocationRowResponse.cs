namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetAllocationRowResponse(
    Guid BudgetLineId,
    int SequenceOrder,
    Guid DepartmentId,
    string DepartmentName,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    Guid? GlAccountId,
    string? GlAccountCode,
    string? GlAccountName,
    decimal AllocatedAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    string? Notes
);
