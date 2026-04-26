namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed class RejectExpenseRequestRequest
{
    public string Comment { get; set; } = default!;
}
