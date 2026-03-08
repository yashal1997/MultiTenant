namespace MultiTenant.Api.Domain.Entities
{
    public sealed class Tenant
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Status { get; set; } = "ACTIVE"; // ACTIVE / SUSPENDED
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
