namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetListItemResponse(
    Guid BudgetId,
    string Name,
    Guid BusinessUnitId,
    string BusinessUnitName,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal AllocatedTotal,
    decimal SpentTotal,
    decimal RemainingTotal,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
