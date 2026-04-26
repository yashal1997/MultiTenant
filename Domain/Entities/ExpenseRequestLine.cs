using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class ExpenseRequestLine : ITenantEntity
{
    public Guid ExpenseRequestLineId { get; set; }
    public Guid TenantId { get; set; }

    public Guid ExpenseRequestId { get; set; }
    public ExpenseRequest ExpenseRequest { get; set; } = default!;

    public int SequenceOrder { get; set; }

    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }

    public Guid? ExpenseCategoryId { get; set; }
    public ExpenseCategory? LineExpenseCategory { get; set; }

    public Guid? GlAccountId { get; set; }
    public GlAccount? GlAccount { get; set; }

    public Guid? VendorId { get; set; }
    public Vendor? LineVendor { get; set; }
}
