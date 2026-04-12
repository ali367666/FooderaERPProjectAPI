using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.Delete;

public record DeleteOrderLineCommand(int Id) : IRequest<OrderResponse>;