using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class NotificationSetting : ITenantEntity
{
    public Guid NotificationSettingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public bool EmailExpenseSubmitted { get; set; } = true;
    public bool EmailExpenseApproved { get; set; } = true;
    public bool EmailExpenseRejected { get; set; } = true;
    public bool EmailPendingApprovalsDigest { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
