namespace MultiTenant.Api.Contracts.BusinessUnits;

public sealed record UpdateBusinessUnitRequest(string Name, string? Description, bool IsActive);
