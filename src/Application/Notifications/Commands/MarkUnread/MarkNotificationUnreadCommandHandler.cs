using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Commands.MarkUnread;

public class MarkNotificationUnreadCommandHandler : IRequestHandler<MarkNotificationUnreadCommand, BaseResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationUnreadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        MarkNotificationUnreadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId <= 0)
            return BaseResponse.Fail("Invalid user context.");

        var n = request.CompanyId is > 0
            ? await _notificationRepository.GetByIdAsync(request.Id, userId, request.CompanyId.Value, cancellationToken)
            : await _notificationRepository.GetByIdForUserAsync(request.Id, userId, cancellationToken);
        if (n == null)
            return BaseResponse.Fail("Notification not found.");

        n.IsRead = false;
        _notificationRepository.Update(n);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok();
    }
}
