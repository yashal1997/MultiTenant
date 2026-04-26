using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

/// <summary>Employee expense / spend request with optional multi-line detail and workflow-based approvals.</summary>
public sealed class ExpenseRequest : ITenantEntity
{
    public Guid ExpenseRequestId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Human-readable id, e.g. REQ-2025-0001. Unique per tenant.</summary>
    public string RequestNumber { get; set; } = default!;

    public Guid SubmittedByUserId { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public ExpenseRequestExpenseType ExpenseType { get; set; } = ExpenseRequestExpenseType.SelfPaidReimbursement;

    /// <summary>Optional client reference; no FK until a Project entity exists.</summary>
    public string? ProjectId { get; set; }

    /// <summary>When <see cref="ExpenseRequestFundingType.SpecialApproval"/>, <see cref="BudgetId"/> must remain null.</summary>
    public ExpenseRequestFundingType FundingType { get; set; } = ExpenseRequestFundingType.BudgetedExpense;

    public ExpenseRequestStatus Status { get; set; } = ExpenseRequestStatus.Draft;

    /// <summary>Sum of line amounts (denormalized for queries).</summary>
    public decimal TotalAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid? ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? BusinessUnitId { get; set; }
    public BusinessUnit? BusinessUnit { get; set; }

    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }

    /// <summary>Workflow template chosen at submit time (copied into <see cref="Approvals"/>).</summary>
    public Guid? WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }

    /// <summary>1-based step awaiting action when <see cref="Status"/> is <see cref="ExpenseRequestStatus.PendingApproval"/>; null otherwise.</summary>
    public int? CurrentApprovalStepSequence { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ExpenseRequestLine> Lines { get; set; } = new List<ExpenseRequestLine>();
    public ICollection<ExpenseRequestApproval> Approvals { get; set; } = new List<ExpenseRequestApproval>();
}
