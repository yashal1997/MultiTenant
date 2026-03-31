namespace MultiTenant.Api.Contracts.Notifications;

public sealed record NotificationSettingsResponse(
    Guid NotificationSettingId,
    bool EmailExpenseSubmitted,
    bool EmailExpenseApproved,
    bool EmailExpenseRejected,
    bool EmailPendingApprovalsDigest,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
