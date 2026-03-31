namespace MultiTenant.Api.Domain.Entities;

/// <summary>
/// One approval hop in order. Lower <see cref="Sequence"/> runs first.
/// </summary>
public sealed class WorkflowStep
{
    public Guid WorkflowStepId { get; set; }
    public Guid WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = default!;

    /// <summary>1-based order in the chain.</summary>
    public int Sequence { get; set; }

    /// <summary>Identity user id; must be an active member of the tenant.</summary>
    public Guid ApproverUserId { get; set; }
}
