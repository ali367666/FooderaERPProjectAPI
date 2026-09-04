using Application.Orders.Dtos;
using MediatR;

namespace Application.OrderLines.Commands.TimeBased;

public record StartTimeBasedLineCommand(int OrderLineId) : IRequest<OrderResponse>;
