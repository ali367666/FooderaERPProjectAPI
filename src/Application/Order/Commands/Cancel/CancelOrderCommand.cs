using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Cancel;

public record CancelOrderCommand(int OrderId, string? Reason = null, string? Note = null) : IRequest<OrderResponse>;
