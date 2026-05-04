namespace MultiTenant.Api.Contracts.ExpenseRequests;

/// <summary>KPI cards + summary counts for the expense dashboard (see ExpenseLinx-style UI).</summary>
public sealed record ExpenseRequestDashboardOverview(
    ExpenseRequestDashboardCounts Counts,
    ExpenseRequestDashboardKpi Pending,
    ExpenseRequestDashboardKpi Approved,
    ExpenseRequestDashboardKpi Completed,
    ExpenseRequestDashboardSpend CurrentYearSpending,
    ExpenseRequestDashboardQuickLinks QuickLinks
);

public sealed record ExpenseRequestDashboardKpi(
    int Count,
    /// <summary>Approximate trend vs prior 30-day window (submissions still in this status).</summary>
    decimal? TrendPercent
);

public sealed record ExpenseRequestDashboardSpend(
    decimal Amount,
    string CurrencyCode,
    decimal? TrendPercent
);

public sealed record ExpenseRequestDashboardQuickLinks(
    string PendingFilterUrl,
    string ApprovedFilterUrl,
    string CompletedFilterUrl,
    string AllFilterUrl,
    string CreateRequestUrl
);
