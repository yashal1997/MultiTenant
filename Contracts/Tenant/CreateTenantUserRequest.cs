namespace MultiTenant.Api.Contracts.Tenant
{
    public sealed class CreateTenantUserRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;

        /// <summary>Department from the add-user / profile form (optional but typical).</summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>Optional; when set must belong to <see cref="DepartmentId"/> if that is also set.</summary>
        public Guid? BusinessUnitId { get; set; }

        /// <summary>Stored on Identity user (<see cref="Microsoft.AspNetCore.Identity.IdentityUser{TKey}.PhoneNumber"/>).</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Per-tenant role label (e.g. job title).</summary>
        public string? JobTitle { get; set; }
    }
}
