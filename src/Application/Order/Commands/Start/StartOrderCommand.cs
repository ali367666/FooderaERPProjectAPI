using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Start;

public record StartOrderCommand(int OrderId) : IRequest<OrderResponse>;
