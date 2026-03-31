namespace MultiTenant.Api.Contracts.Workflows;

public sealed record WorkflowStepResponse(
    Guid WorkflowStepId,
    int Sequence,
    Guid ApproverUserId,
    string? ApproverEmail,
    string? ApproverFullName
);
