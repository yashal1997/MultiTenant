using Microsoft.AspNetCore.Identity;

namespace MultiTenant.Api.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    // Platform admin can access everything (not tenant-bound)
    public bool IsPlatformAdmin { get; set; } = false;
}
