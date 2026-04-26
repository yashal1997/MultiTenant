using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed record ExpenseRequestApprovalResponse(
    Guid ExpenseRequestApprovalId,
    int StepSequence,
    Guid ApproverUserId,
    string? ApproverEmail,
    string? ApproverFullName,
    ExpenseRequestApprovalStatus StepStatus,
    DateTime? ActionAtUtc,
    string? Comment
);
