using Application.Order.Dtos.Request;
using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Update;

public record UpdateOrderCommand(UpdateOrderRequest Request) : IRequest<OrderResponse>;