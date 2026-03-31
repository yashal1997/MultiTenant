namespace MultiTenant.Api.Contracts.Departments;

public sealed record UpdateDepartmentRequest(string Name, string? Description, bool IsActive);
