namespace MultiTenant.Api.Contracts.Notifications;

public sealed record UpdateNotificationSettingsRequest(
    bool EmailExpenseSubmitted,
    bool EmailExpenseApproved,
    bool EmailExpenseRejected,
    bool EmailPendingApprovalsDigest,
    bool? EmailNotificationsEnabled,
    bool? PushNotificationsEnabled
);
