using Application.Common.Responce;
using MediatR;

namespace Application.Notifications.Commands.MarkRead;

public record MarkNotificationReadCommand(int Id, int? CompanyId) : IRequest<BaseResponse>;
