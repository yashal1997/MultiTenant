namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed record ExpenseRequestListEnvelope(
    ExpenseRequestDashboardCounts Counts,
    IReadOnlyList<ExpenseRequestListItemResponse> Items
);
