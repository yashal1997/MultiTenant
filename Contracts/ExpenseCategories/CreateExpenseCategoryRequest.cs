namespace MultiTenant.Api.Contracts.ExpenseCategories;

public sealed record CreateExpenseCategoryRequest(
    string Name,
    string? Description,
    string GlCode
);

