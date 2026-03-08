namespace MultiTenant.Api.Contracts.Auth;

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresUtc { get; set; }
}
