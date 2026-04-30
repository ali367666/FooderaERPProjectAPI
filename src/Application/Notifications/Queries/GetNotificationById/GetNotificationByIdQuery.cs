using Application.Common.Responce;
using Application.Notifications.Dtos.Response;
using MediatR;

namespace Application.Notifications.Queries.GetNotificationById;

public record GetNotificationByIdQuery(int Id, int? CompanyId)
    : IRequest<BaseResponse<NotificationResponse>>;
