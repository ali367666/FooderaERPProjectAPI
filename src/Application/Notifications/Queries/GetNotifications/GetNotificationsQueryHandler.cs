using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Notifications.Dtos.Response;
using Domain.Entities;
using MediatR;

namespace Application.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, BaseResponse<List<NotificationResponse>>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId <= 0)
            return BaseResponse<List<NotificationResponse>>.Fail("Invalid user context.");

        var items = request.CompanyId is > 0
            ? await _notificationRepository.GetByUserIdAsync(userId, request.CompanyId.Value, cancellationToken)
            : await _notificationRepository.GetAllForUserAsync(userId, cancellationToken);
        var response = items.Select(Map).ToList();
        return BaseResponse<List<NotificationResponse>>.Ok(response);
    }

    private static NotificationResponse Map(Notification n) => new()
    {
        Id = n.Id,
        CompanyId = n.CompanyId,
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        Type = n.Type,
        ReferenceId = n.ReferenceId,
        ReferenceType = n.ReferenceType,
        CreatedAtUtc = n.CreatedAtUtc,
    };
}
