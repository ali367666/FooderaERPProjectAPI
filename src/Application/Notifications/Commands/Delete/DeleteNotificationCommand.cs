using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Commands.Delete;

public record DeleteNotificationCommand(int Id, int? CompanyId) : IRequest<BaseResponse>;
