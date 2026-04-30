using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task CreateAsync(
        int userId,
        int companyId,
        string title,
        string message,
        string type,
        int? referenceId = null,
        string? referenceType = null,
        int? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var notification = new Notification
        {
            UserId = userId,
            CompanyId = companyId,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            IsRead = false,
            CreatedAtUtc = utcNow,
            CreatedByUserId = createdByUserId ?? userId,
        };

        _logger.LogInformation(
            "Creating notification: UserId={UserId}, CompanyId={CompanyId}, Type={Type}, ReferenceId={ReferenceId}, ReferenceType={ReferenceType}",
            userId,
            companyId,
            type,
            referenceId,
            referenceType);

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification saved: Id={NotificationId}, UserId={UserId}, CompanyId={CompanyId}",
            notification.Id,
            userId,
            companyId);
    }

    public async Task MarkAsReadAsync(
        int notificationId,
        int userId,
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            userId,
            companyId,
            cancellationToken);

        if (notification is null)
            throw new Exception("Notification tapılmadı.");

        if (notification.IsRead)
            return;

        notification.IsRead = true;

        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(
            userId,
            companyId,
            cancellationToken);
    }
}