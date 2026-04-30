using Application.Common.Responce;
using MediatR;

namespace Application.Order.Commands.Serve;

public record ServeOrderCommand(int OrderId) : IRequest<BaseResponse>;
