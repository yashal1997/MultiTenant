namespace MultiTenant.Api.Contracts.Workflows;



public sealed record WorkflowListItemResponse(

    Guid WorkflowId,

    string Name,

    string? Description,

    bool ApplyToAllBusinessUnits,

    bool ApplyToAllDepartments,

    bool ApplyToAllExpenseCategories,

    int ScopedBusinessUnitCount,

    int ScopedDepartmentCount,

    int ScopedExpenseCategoryCount,

    decimal? ApprovalThresholdAmount,

    bool IsActive,

    int StepCount,

    DateTime CreatedAtUtc,

    DateTime? UpdatedAtUtc

);


