namespace MultiTenant.Api.Contracts.Departments;

public sealed record DepartmentResponse(
    Guid DepartmentId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
