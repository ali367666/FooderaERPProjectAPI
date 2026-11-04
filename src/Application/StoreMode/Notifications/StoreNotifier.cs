using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.StoreMode.Notifications;

/// <summary>
/// Tells the people who can act on something that it happened (low stock, a transfer on its way, a count
/// with differences). It never fails the operation that triggered it.
/// </summary>
public sealed class StoreNotifier
{
    private const int MaxRecipients = 30;

    private readonly IIdentityAdminService _identity;
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StoreNotifier> _logger;
    private readonly IStoreRealtime? _realtime;

    public StoreNotifier(
        IIdentityAdminService identity,
        INotificationService notifications,
        ICurrentUserService currentUser,
        ILogger<StoreNotifier> logger,
        IStoreRealtime? realtime = null)
    {
        _realtime = realtime;
        _identity = identity;
        _notifications = notifications;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task NotifyHoldersAsync(
        string permission, string title, string message, string type, int? referenceId, string? referenceType, CancellationToken ct)
    {
        try
        {
            var companyId = _currentUser.CompanyId;
            var userIds = await _identity.GetUserIdsWithPermissionAsync(companyId, permission, ct);
            foreach (var userId in userIds.Take(MaxRecipients))
                await _notifications.CreateAsync(
                    userId, companyId, title, message, type, referenceId, referenceType, _currentUser.UserId, ct);

            if (_realtime is not null)
                await _realtime.PublishAsync(companyId, "storeAlert", new { title, message, type }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Store notification failed: {Title}", title);
        }
    }
}
