namespace MultiTenant.Api.Contracts.Tenant;

public sealed class UpdateTenantUserPreferencesRequest
{
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
}
