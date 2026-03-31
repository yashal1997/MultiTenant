namespace MultiTenant.Api.Contracts.Workflows;



public sealed class CreateWorkflowRequest

{

    public string Name { get; set; } = default!;

    public string? Description { get; set; }



    /// <summary>When false, <see cref="BusinessUnitIds"/> must list allowed business units.</summary>

    public bool ApplyToAllBusinessUnits { get; set; } = true;



    /// <summary>When false, <see cref="DepartmentIds"/> must list allowed departments.</summary>

    public bool ApplyToAllDepartments { get; set; } = true;



    /// <summary>When false, <see cref="ExpenseCategoryIds"/> must list allowed categories.</summary>

    public bool ApplyToAllExpenseCategories { get; set; } = true;



    public List<Guid> BusinessUnitIds { get; set; } = new();

    public List<Guid> DepartmentIds { get; set; } = new();

    public List<Guid> ExpenseCategoryIds { get; set; } = new();



    /// <summary>

    /// Optional. Amounts **below** this skip the approval chain when scope matches.

    /// Omit or null for no amount bypass.

    /// </summary>

    public decimal? ApprovalThresholdAmount { get; set; }



    /// <summary>Ordered approval chain; <see cref="WorkflowStepInput.Sequence"/> defines order (1 = first approver).</summary>

    public List<WorkflowStepInput> Steps { get; set; } = new();

}


