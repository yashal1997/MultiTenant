using MultiTenant.Api.Domain.Entities;

namespace MultiTenant.Api.Contracts.ExpenseRequests;

public sealed class CreateExpenseRequestRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ExpenseRequestExpenseType ExpenseType { get; set; } = ExpenseRequestExpenseType.SelfPaidReimbursement;
    public string? ProjectId { get; set; }
    public ExpenseRequestFundingType FundingType { get; set; } = ExpenseRequestFundingType.BudgetedExpense;
    public string? CurrencyCode { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public Guid? BudgetId { get; set; }
    public List<ExpenseRequestLineInput> Lines { get; set; } = new();
}
