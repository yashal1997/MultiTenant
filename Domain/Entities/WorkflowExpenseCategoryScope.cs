using MultiTenant.Api.Domain.Common;



namespace MultiTenant.Api.Domain.Entities;



/// <summary>When <see cref="Workflow.ApplyToAllExpenseCategories"/> is false, the workflow applies only to these categories.</summary>

public sealed class WorkflowExpenseCategoryScope : ITenantEntity

{

    public Guid WorkflowExpenseCategoryScopeId { get; set; }

    public Guid TenantId { get; set; }



    public Guid WorkflowId { get; set; }

    public Workflow Workflow { get; set; } = default!;



    public Guid ExpenseCategoryId { get; set; }

    public ExpenseCategory ExpenseCategory { get; set; } = default!;

}


