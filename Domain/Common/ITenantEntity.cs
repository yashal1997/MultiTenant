namespace MultiTenant.Api.Domain.Common
{
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
