namespace MultiTenant.Api.Contracts.Departments;

public sealed record CreateDepartmentRequest(string Name, string? Description);
