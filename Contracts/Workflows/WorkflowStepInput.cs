namespace MultiTenant.Api.Contracts.Workflows;

/// <summary>
/// One hop in the chain. <see cref="Sequence"/> is 1-based; must be unique per workflow.
/// In JSON, approverUserId must be a quoted string (e.g. "35283626-1a2b-4c3d-9e8f-001122334455").
/// Unquoted GUIDs are read as numbers and fail when the first character is a digit.
/// </summary>
public sealed class WorkflowStepInput
{
    public int Sequence { get; set; }

    public Guid ApproverUserId { get; set; }
}
