namespace MultiTenant.Api.Domain.Entities;

/// <summary>
/// Tenant membership. <see cref="DepartmentId"/> matches the profile “Department” field (optional).
/// <see cref="BusinessUnitId"/> is optional; when set it must belong to that department and we store both.
/// </summary>
public sealed class TenantUser
{
    public Guid TenantUserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? BusinessUnitId { get; set; }
    public BusinessUnit? BusinessUnit { get; set; }

    public string? JobTitle { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
