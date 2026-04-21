namespace MultiTenant.Api.Contracts.Platform;

public sealed class CreateTenantRequest
{
    public string Name { get; set; } = default!;
}
