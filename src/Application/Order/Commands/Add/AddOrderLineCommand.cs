using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.Add;

public record AddOrderLineCommand(AddOrderLineRequest Request) : IRequest<OrderResponse>;