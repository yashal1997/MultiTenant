namespace MultiTenant.Api.Contracts.ExpenseCategories;

public sealed record UpdateExpenseCategoryRequest(
    string Name,
    string? Description,
    string GlCode,
    bool IsActive
);

