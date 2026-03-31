namespace MultiTenant.Api.Contracts.BusinessUnits;

public sealed record CreateBusinessUnitRequest(Guid DepartmentId, string Name, string? Description);
