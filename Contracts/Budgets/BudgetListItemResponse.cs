using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetListItemResponse(
    Guid BudgetId,
    string Name,
    int FiscalYear,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    BudgetStatus Status,
    string CurrencyCode,
    decimal? TotalAmount,
    decimal AllocatedTotal,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
