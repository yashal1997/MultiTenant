namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetLineResponse(
    Guid BudgetLineId,
    int SequenceOrder,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? BusinessUnitId,
    string? BusinessUnitName,
    Guid? ExpenseCategoryId,
    string? ExpenseCategoryName,
    Guid? GlAccountId,
    string? GlAccountCode,
    string? GlAccountName,
    decimal AllocatedAmount,
    string? Notes
);
