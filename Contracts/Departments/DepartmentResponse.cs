namespace MultiTenant.Api.Contracts.Departments;

public sealed record DepartmentResponse(
    Guid DepartmentId,
    string Name,
    string DepartmentCode,
    Guid? HeadOfDepartmentUserId,
    string? HeadOfDepartmentName,
    Guid? PrimaryBusinessUnitId,
    string? PrimaryBusinessUnitName,
    int EmployeeCount,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
