using Application.Common.Responce;
using Application.Notifications.Dtos.Response;
using MediatR;

namespace Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(int? CompanyId)
    : IRequest<BaseResponse<List<NotificationResponse>>>;
