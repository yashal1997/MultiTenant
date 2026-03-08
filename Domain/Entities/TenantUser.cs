namespace MultiTenant.Api.Domain.Entities;

public sealed class TenantUser
{
    public Guid TenantUserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
