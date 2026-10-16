using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record UpdateCounterpartyCommand(UpdateCounterpartyRequest Request) : IRequest<CounterpartyResponse>;
