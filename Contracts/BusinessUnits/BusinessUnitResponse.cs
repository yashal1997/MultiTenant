namespace MultiTenant.Api.Contracts.BusinessUnits;

public sealed record BusinessUnitResponse(
    Guid BusinessUnitId,
    Guid DepartmentId,
    string DepartmentName,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
