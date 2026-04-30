using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Serve;

public record ServeOrderCommand(int OrderId) : IRequest<OrderResponse>;
