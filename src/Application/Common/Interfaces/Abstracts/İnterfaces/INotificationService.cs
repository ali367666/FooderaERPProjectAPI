namespace Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateAsync(
        int userId,
        int companyId,
        string title,
        string message,
        string type,
        int? referenceId = null,
        string? referenceType = null,
        int? createdByUserId = null,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        int notificationId,
        int userId,
        int companyId,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken = default);
}