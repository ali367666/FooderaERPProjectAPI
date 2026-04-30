using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Cancel;

public record CancelOrderCommand(int OrderId) : IRequest<OrderResponse>;
