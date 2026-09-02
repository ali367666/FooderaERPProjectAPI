using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.SetHold;

public record SetOrderLineHoldCommand(int OrderLineId, int? HoldMinutes) : IRequest<OrderResponse>;
