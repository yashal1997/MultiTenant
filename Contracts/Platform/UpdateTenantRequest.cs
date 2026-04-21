namespace MultiTenant.Api.Contracts.Platform;

public sealed class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? Status { get; set; }
}
