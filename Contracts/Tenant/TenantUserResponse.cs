namespace MultiTenant.Api.Contracts.Tenant;

public sealed class TenantUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    public Guid? LineManagerUserId { get; set; }
    public string? LineManagerName { get; set; }
    public bool IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public string? BusinessUnitName { get; set; }
}
