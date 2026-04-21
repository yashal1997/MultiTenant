namespace MultiTenant.Api.Contracts.Tenant;

public sealed class UpdateTenantUserRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    public Guid? LineManagerUserId { get; set; }

    /// <summary>
    /// When true, <see cref="DepartmentId"/> and <see cref="BusinessUnitId"/> are applied
    /// (use null for either to clear that part of the assignment). If only department is set, business unit is cleared.
    /// </summary>
    public bool? UpdateOrganization { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public bool? IsActive { get; set; }
    public string? NewPassword { get; set; }
}
