namespace MultiTenant.Api.Contracts.BusinessUnits;

public sealed record BusinessUnitResponse(
    Guid BusinessUnitId,
    Guid? DepartmentId,
    string? DepartmentName,
    string Name,
    string UnitCode,
    Guid? HeadOfUnitUserId,
    string? HeadOfUnitName,
    int DepartmentCount,
    int EmployeeCount,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
