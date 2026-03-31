namespace MultiTenant.Api.Contracts.Workflows;



public sealed record WorkflowScopeResponse(

    bool ApplyToAllBusinessUnits,

    bool ApplyToAllDepartments,

    bool ApplyToAllExpenseCategories,

    IReadOnlyList<WorkflowScopeItemResponse> BusinessUnits,

    IReadOnlyList<WorkflowScopeItemResponse> Departments,

    IReadOnlyList<WorkflowScopeItemResponse> ExpenseCategories

);



public sealed record WorkflowScopeItemResponse(Guid Id, string Name);


