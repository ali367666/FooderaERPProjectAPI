using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Notifications.Dtos.Response;
using Domain.Entities;
using MediatR;

namespace Application.Notifications.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler
    : IRequestHandler<GetNotificationByIdQuery, BaseResponse<NotificationResponse>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationByIdQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<NotificationResponse>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId <= 0)
            return BaseResponse<NotificationResponse>.Fail("Invalid user context.");

        var n = request.CompanyId is > 0
            ? await _notificationRepository.GetByIdAsync(request.Id, userId, request.CompanyId.Value, cancellationToken)
            : await _notificationRepository.GetByIdForUserAsync(request.Id, userId, cancellationToken);
        if (n == null)
            return BaseResponse<NotificationResponse>.Fail("Notification not found.");

        return BaseResponse<NotificationResponse>.Ok(Map(n));
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
