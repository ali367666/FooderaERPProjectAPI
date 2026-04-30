using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Queries.GetUnreadCount;

public record GetUnreadNotificationCountQuery(int? CompanyId) : IRequest<BaseResponse<int>>;
