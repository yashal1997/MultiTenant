namespace MultiTenant.Api.Domain.Entities;

/// <summary>How the expense is paid / recorded (header-level).</summary>
public enum ExpenseRequestExpenseType
{
    SelfPaidReimbursement,
    PayToVendor
}
