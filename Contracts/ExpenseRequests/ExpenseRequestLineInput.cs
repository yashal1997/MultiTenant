namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed class ExpenseRequestLineInput
{
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public Guid? GlAccountId { get; set; }
    public Guid? VendorId { get; set; }
}
