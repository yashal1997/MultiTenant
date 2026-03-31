namespace MultiTenant.Api.Contracts.Workflows;



public sealed record WorkflowDetailResponse(

    Guid WorkflowId,

    string Name,

    string? Description,

    WorkflowScopeResponse Scope,

    decimal? ApprovalThresholdAmount,

    bool IsActive,

    IReadOnlyList<WorkflowStepResponse> Steps,

    DateTime CreatedAtUtc,

    DateTime? UpdatedAtUtc

);


