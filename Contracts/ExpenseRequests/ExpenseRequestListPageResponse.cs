namespace MultiTenant.Api.Contracts.ExpenseRequests;

/// <summary>Paginated &quot;My requests&quot; / tenant list with summary chips matching dashboard counts.</summary>
public sealed record ExpenseRequestListPageResponse(
    ExpenseRequestDashboardCounts SummaryCounts,
    IReadOnlyList<ExpenseRequestListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);
