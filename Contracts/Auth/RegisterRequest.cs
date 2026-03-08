namespace MultiTenant.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FullName { get; set; } = default!;

    // You can create a new tenant on register OR join existing later
    public Guid? TenantId { get; set; }   // optional
    public string? TenantName { get; set; } // optional (if creating new)
}
