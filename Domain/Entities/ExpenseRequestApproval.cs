using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

/// <summary>One approver hop for an expense request (copied from workflow at submit).</summary>
public sealed class ExpenseRequestApproval : ITenantEntity
{
    public Guid ExpenseRequestApprovalId { get; set; }
    public Guid TenantId { get; set; }

    public Guid ExpenseRequestId { get; set; }
    public ExpenseRequest ExpenseRequest { get; set; } = default!;

    /// <summary>1-based order (matches workflow step sequence).</summary>
    public int StepSequence { get; set; }

    public Guid ApproverUserId { get; set; }

    public ExpenseRequestApprovalStatus StepStatus { get; set; } = ExpenseRequestApprovalStatus.Pending;

    public DateTime? ActionAtUtc { get; set; }
    public string? Comment { get; set; }
}
