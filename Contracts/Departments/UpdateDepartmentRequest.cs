namespace MultiTenant.Api.Contracts.Departments;

public sealed record UpdateDepartmentRequest(
    string Name,
    string DepartmentCode,
    Guid? HeadOfDepartmentUserId,
    Guid? PrimaryBusinessUnitId,
    string? Description,
    bool IsActive
);
