namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetDetailResponse(
    Guid BudgetId,
    string Name,
    string? Description,
    Guid BusinessUnitId,
    string BusinessUnitName,
    int FiscalYear,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal AllocatedTotal,
    decimal SpentTotal,
    decimal RemainingTotal,
    bool IsActive,
    IReadOnlyList<BudgetDepartmentAllocationResponse> DepartmentAllocations,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
