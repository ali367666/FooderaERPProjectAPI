using Application.Orders.Dtos;
using Application.Orders.Dtos.Request;
using MediatR;

namespace Application.Orders.Commands.Pay;

public record PayOrderCommand(int OrderId, PayOrderRequest Request) : IRequest<OrderResponse>;
