using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Commands.Delete;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, BaseResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteNotificationCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId <= 0)
            return BaseResponse.Fail("Invalid user context.");

        var ok = request.CompanyId is > 0
            ? await _notificationRepository.DeleteAsync(request.Id, userId, request.CompanyId.Value, cancellationToken)
            : await _notificationRepository.DeleteForUserAsync(request.Id, userId, cancellationToken);
        if (!ok)
            return BaseResponse.Fail("Notification not found.");

        return BaseResponse.Ok();
    }
}
