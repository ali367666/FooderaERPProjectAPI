using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.TimeBased;

public record StopTimeBasedLineCommand(int OrderLineId) : IRequest<OrderResponse>;
