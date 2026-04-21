namespace MultiTenant.Api.Contracts.ExpenseCategories;

public sealed record CreateExpenseCategoryRequest(
    string Name,
    string CategoryCode,
    string? Description,
    string GlCode
);

