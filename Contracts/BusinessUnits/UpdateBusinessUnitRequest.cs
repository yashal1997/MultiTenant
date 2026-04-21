namespace MultiTenant.Api.Contracts.BusinessUnits;

public sealed record UpdateBusinessUnitRequest(
    Guid? DepartmentId,
    string Name,
    string UnitCode,
    Guid? HeadOfUnitUserId,
    string? Description,
    bool IsActive
);
