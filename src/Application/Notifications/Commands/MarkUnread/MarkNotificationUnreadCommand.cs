using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Commands.MarkUnread;

public record MarkNotificationUnreadCommand(int Id, int? CompanyId) : IRequest<BaseResponse>;
