namespace MultiTenant.Api.Contracts.Budgets;

public sealed record BudgetDepartmentAllocationResponse(
    Guid DepartmentId,
    string DepartmentName,
    decimal AllocatedTotal,
    decimal SpentTotal,
    decimal RemainingTotal,
    IReadOnlyList<BudgetAllocationRowResponse> Allocations
);
