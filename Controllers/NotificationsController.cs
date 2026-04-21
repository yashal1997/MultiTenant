using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Notifications;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public NotificationsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(NotificationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings()
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "Invalid user." });

        var settings = await GetOrCreateSettingsAsync(userId);
        return Ok(ToResponse(settings));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(NotificationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNotificationSettingsRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "Invalid user." });

        var settings = await GetOrCreateSettingsAsync(userId);

        settings.EmailExpenseSubmitted = request.EmailExpenseSubmitted;
        settings.EmailExpenseApproved = request.EmailExpenseApproved;
        settings.EmailExpenseRejected = request.EmailExpenseRejected;
        settings.EmailPendingApprovalsDigest = request.EmailPendingApprovalsDigest;
        if (request.EmailNotificationsEnabled.HasValue)
            settings.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
        if (request.PushNotificationsEnabled.HasValue)
            settings.PushNotificationsEnabled = request.PushNotificationsEnabled.Value;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(settings));
    }

    private async Task<NotificationSetting> GetOrCreateSettingsAsync(Guid userId)
    {
        var settings = await _db.NotificationSettings
            .FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId!.Value && x.UserId == userId);

        if (settings is not null)
            return settings;

        settings = new NotificationSetting
        {
            NotificationSettingId = Guid.NewGuid(),
            TenantId = _tenant.TenantId!.Value,
            UserId = userId,
            EmailExpenseSubmitted = true,
            EmailExpenseApproved = true,
            EmailExpenseRejected = true,
            EmailPendingApprovalsDigest = true,
            EmailNotificationsEnabled = true,
            PushNotificationsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.NotificationSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static NotificationSettingsResponse ToResponse(NotificationSetting x) => new(
        x.NotificationSettingId,
        x.EmailExpenseSubmitted,
        x.EmailExpenseApproved,
        x.EmailExpenseRejected,
        x.EmailPendingApprovalsDigest,
        x.EmailNotificationsEnabled,
        x.PushNotificationsEnabled,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );
}
