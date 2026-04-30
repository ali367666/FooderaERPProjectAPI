using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadNotificationCountQueryHandler
    : IRequestHandler<GetUnreadNotificationCountQuery, BaseResponse<int>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId <= 0)
            return BaseResponse<int>.Fail("Invalid user context.");

        var count = request.CompanyId is > 0
            ? await _notificationRepository.GetUnreadCountAsync(userId, request.CompanyId.Value, cancellationToken)
            : await _notificationRepository.GetUnreadCountAllCompaniesAsync(userId, cancellationToken);
        return BaseResponse<int>.Ok(count);
    }
}
