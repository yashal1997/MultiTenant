namespace MultiTenant.Api.Contracts.Auth;

public sealed class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public List<TenantSummary> Tenants { get; set; } = new();

    public string BaseToken { get; set; } = default!;
    public DateTime BaseTokenExpiresUtc { get; set; }
}

public sealed class TenantSummary
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
}
