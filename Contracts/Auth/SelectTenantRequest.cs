namespace MultiTenant.Api.Contracts.Auth;

public sealed class SelectTenantRequest
{
    public Guid TenantId { get; set; }
}
