namespace MultiTenant.Api.Contracts.Notifications;

public sealed record NotificationSettingsResponse(
    Guid NotificationSettingId,
    bool EmailExpenseSubmitted,
    bool EmailExpenseApproved,
    bool EmailExpenseRejected,
    bool EmailPendingApprovalsDigest,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
