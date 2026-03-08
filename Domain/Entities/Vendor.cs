using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class Vendor : ITenantEntity
{
    public Guid VendorId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
