using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task CreateAsync(
        int userId,
        int companyId,
        string title,
        string message,
        string type,
        int? referenceId = null,
        string? referenceType = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            CompanyId = companyId,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            IsRead = false
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
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