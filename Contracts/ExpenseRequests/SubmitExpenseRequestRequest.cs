namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed class SubmitExpenseRequestRequest
{
    /// <summary>Workflow to use for the approval chain (must match request scope and be active).</summary>
    public Guid WorkflowId { get; set; }
}
