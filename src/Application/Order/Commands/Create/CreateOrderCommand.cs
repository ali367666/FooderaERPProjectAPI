using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Create;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderResponse>;