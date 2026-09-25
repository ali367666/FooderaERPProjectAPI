using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.SetOrderHold;

/// <summary>
/// Parks or releases the WHOLE order (as opposed to <see cref="Application.OrderLines.Commands.SetHold.SetOrderLineHoldCommand"/>,
/// which only holds one line). A held order's lines are hidden from the kitchen queue and
/// excluded from kitchen ticket printing until released.
/// </summary>
public record SetOrderHoldCommand(int OrderId, bool Hold) : IRequest<OrderResponse>;
