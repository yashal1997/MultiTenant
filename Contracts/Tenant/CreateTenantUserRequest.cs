namespace MultiTenant.Api.Contracts.Tenant
{
    public sealed class CreateTenantUserRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }
}
