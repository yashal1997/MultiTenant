using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class GlAccount : ITenantEntity
{
    public Guid GlAccountId { get; set; }
    public Guid TenantId { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

