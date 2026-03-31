namespace MultiTenant.Api.Contracts.ExpenseCategories;

public sealed record ExpenseCategoryResponse(
    Guid ExpenseCategoryId,
    string Name,
    string? Description,
    bool IsActive,
    Guid GlAccountId,
    string GlCode,
    string GlAccountName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

