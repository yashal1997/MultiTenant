using MultiTenant.Api.Domain.Common;



namespace MultiTenant.Api.Domain.Entities;



/// <summary>

/// Defines a sequential approval chain (e.g. line manager → manager → finance).

/// </summary>

public sealed class Workflow : ITenantEntity

{

    public Guid WorkflowId { get; set; }

    public Guid TenantId { get; set; }



    public string Name { get; set; } = default!;

    public string? Description { get; set; }



    /// <summary>When true, every business unit is in scope; otherwise only <see cref="BusinessUnitScopes"/>.</summary>

    public bool ApplyToAllBusinessUnits { get; set; } = true;



    /// <summary>When true, every department is in scope; otherwise only <see cref="DepartmentScopes"/>.</summary>

    public bool ApplyToAllDepartments { get; set; } = true;



    /// <summary>When true, every expense category is in scope; otherwise only <see cref="ExpenseCategoryScopes"/>.</summary>

    public bool ApplyToAllExpenseCategories { get; set; } = true;



    /// <summary>

    /// When set, amounts **strictly below** this value do not require the approval chain once scope matches.

    /// When null, there is no amount bypass for matching expenses.

    /// </summary>

    public decimal? ApprovalThresholdAmount { get; set; }



    public bool IsActive { get; set; } = true;



    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }



    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

    public ICollection<WorkflowBusinessUnitScope> BusinessUnitScopes { get; set; } = new List<WorkflowBusinessUnitScope>();

    public ICollection<WorkflowDepartmentScope> DepartmentScopes { get; set; } = new List<WorkflowDepartmentScope>();

    public ICollection<WorkflowExpenseCategoryScope> ExpenseCategoryScopes { get; set; } = new List<WorkflowExpenseCategoryScope>();



    /// <summary>

    /// Whether this expense context falls under the workflow's BU / department / category rules.

    /// Requires scope collections loaded when the corresponding <c>ApplyToAll*</c> is false.

    /// </summary>

    public bool AppliesToScope(Guid? businessUnitId, Guid? departmentId, Guid? expenseCategoryId)

    {

        if (!ApplyToAllBusinessUnits)

        {

            if (!businessUnitId.HasValue)

                return false;

            if (BusinessUnitScopes.All(x => x.BusinessUnitId != businessUnitId.Value))

                return false;

        }



        if (!ApplyToAllDepartments)

        {

            if (!departmentId.HasValue)

                return false;

            if (DepartmentScopes.All(x => x.DepartmentId != departmentId.Value))

                return false;

        }



        if (!ApplyToAllExpenseCategories)

        {

            if (!expenseCategoryId.HasValue)

                return false;

            if (ExpenseCategoryScopes.All(x => x.ExpenseCategoryId != expenseCategoryId.Value))

                return false;

        }



        return true;

    }



    /// <summary>

    /// True when the expense matches <see cref="AppliesToScope"/> and the amount is not below the optional threshold.

    /// </summary>

    public bool ShouldRunApprovalChain(decimal amount, Guid? businessUnitId, Guid? departmentId, Guid? expenseCategoryId) =>

        AppliesToScope(businessUnitId, departmentId, expenseCategoryId) &&

        (!ApprovalThresholdAmount.HasValue || amount >= ApprovalThresholdAmount.Value);



    /// <summary>

    /// False when <paramref name="amount"/> is strictly below <see cref="ApprovalThresholdAmount"/> (skip approval).

    /// When threshold is null, true (use the chain when this workflow applies).

    /// </summary>

    public bool RequiresApprovalForAmount(decimal amount) =>

        !ApprovalThresholdAmount.HasValue || amount >= ApprovalThresholdAmount.Value;

}


