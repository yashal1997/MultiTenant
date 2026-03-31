namespace MultiTenant.Api.Contracts.Workflows;

/// <summary>Replaces the entire step chain (use to reorder or change approvers).</summary>
public sealed class ReplaceWorkflowStepsRequest
{
    public List<WorkflowStepInput> Steps { get; set; } = new();
}
