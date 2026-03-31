namespace MultiTenant.Api.Contracts.Workflows;

/// <summary>Result of evaluating a hypothetical expense against a workflow (scope + threshold).</summary>
public sealed record WorkflowApprovalEvaluationResponse(
    bool AppliesToScope,
    /// <summary>Amount-only rule: false when amount is strictly below <see cref="ApprovalThresholdAmount"/>.</summary>
    bool RequiresApprovalForAmount,
    /// <summary>True when both scope matches and amount requires approval (run the step chain).</summary>
    bool ShouldRunApprovalChain,
    decimal? ApprovalThresholdAmount,
    /// <summary>Short human-readable explanation for testing.</summary>
    string Message
);
