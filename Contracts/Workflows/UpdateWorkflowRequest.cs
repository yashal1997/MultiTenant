namespace MultiTenant.Api.Contracts.Workflows;

public sealed class UpdateWorkflowRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public bool ApplyToAllBusinessUnits { get; set; } = true;
    public bool ApplyToAllDepartments { get; set; } = true;
    public bool ApplyToAllExpenseCategories { get; set; } = true;

    public List<Guid> BusinessUnitIds { get; set; } = new();
    public List<Guid> DepartmentIds { get; set; } = new();
    public List<Guid> ExpenseCategoryIds { get; set; } = new();

    /// <summary>Null clears the threshold (no amount bypass).</summary>
    public decimal? ApprovalThresholdAmount { get; set; }

    /// <summary>
    /// When non-null, replaces the entire approval chain (same as PUT .../steps).
    /// Omit or null to leave steps unchanged.
    /// </summary>
    public List<WorkflowStepInput>? Steps { get; set; }
}
