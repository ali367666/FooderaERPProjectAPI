using Application.Order.Dtos.Request;
using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.Update;

public record UpdateOrderLineCommand(UpdateOrderLineRequest Request) : IRequest<OrderResponse>;