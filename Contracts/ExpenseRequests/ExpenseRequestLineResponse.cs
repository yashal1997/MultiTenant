namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed record ExpenseRequestLineResponse(
    Guid ExpenseRequestLineId,
    int SequenceOrder,
    string Description,
    decimal Amount,
    Guid? ExpenseCategoryId,
    string? ExpenseCategoryName,
    Guid? GlAccountId,
    string? GlAccountCode,
    string? GlAccountName,
    Guid? VendorId,
    string? VendorName
);
