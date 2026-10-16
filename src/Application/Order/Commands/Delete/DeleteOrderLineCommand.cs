using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.Delete;

/// <param name="Reason">Required when the product was already sent to the kitchen.</param>
public record DeleteOrderLineCommand(int Id, string? Reason = null, string? Note = null) : IRequest<OrderResponse>;