using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed record ExpenseRequestListItemResponse(
    Guid ExpenseRequestId,
    string RequestNumber,
    string Title,
    ExpenseRequestExpenseType ExpenseType,
    string? ProjectId,
    ExpenseRequestFundingType FundingType,
    ExpenseRequestStatus Status,
    decimal TotalAmount,
    string CurrencyCode,
    Guid SubmittedByUserId,
    string? SubmitterEmail,
    string? SubmitterFullName,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc
);
