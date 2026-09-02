using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.SetCounterparty;

public record SetOrderCounterpartyCommand(int OrderId, int? CounterpartyId) : IRequest<OrderResponse>;
