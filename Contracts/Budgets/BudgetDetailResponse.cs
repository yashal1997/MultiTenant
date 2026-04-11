using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetDetailResponse(
    Guid BudgetId,
    string Name,
    string? Description,
    int FiscalYear,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    BudgetStatus Status,
    string CurrencyCode,
    decimal? TotalAmount,
    decimal AllocatedTotal,
    bool IsActive,
    IReadOnlyList<BudgetLineResponse> Lines,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
