namespace MultiTenant.Api.Contracts.Departments;

public sealed record CreateDepartmentRequest(
    string Name,
    string DepartmentCode,
    Guid? HeadOfDepartmentUserId,
    Guid? PrimaryBusinessUnitId,
    string? Description
);
