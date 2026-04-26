namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed record ExpenseRequestDashboardCounts(
    int Total,
    int Draft,
    int PendingApproval,
    int Approved,
    int Rejected,
    int Completed
);
