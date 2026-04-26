namespace MultiTenant.Api.Domain.Entities;

public enum ExpenseRequestStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4
}
