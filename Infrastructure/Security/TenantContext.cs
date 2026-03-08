using MultiTenant.Api.Application.Interfaces;

namespace MultiTenant.Api.Infrastructure.Security;

    public sealed class TenantContext : ITenantContext
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }

