namespace MultiTenant.Api.Contracts.Workflows;

/// <summary>Change approver for this hop without reordering (use PUT …/steps to reorder).</summary>
public sealed record UpdateWorkflowStepRequest(Guid ApproverUserId);
