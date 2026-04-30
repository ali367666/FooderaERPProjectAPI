using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Complete;

public record CompleteOrderCommand(int OrderId) : IRequest<OrderResponse>;
