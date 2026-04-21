namespace MultiTenant.Api.Contracts.Tenant;

public sealed class TenantUserPreferencesResponse
{
    public Guid NotificationSettingId { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
