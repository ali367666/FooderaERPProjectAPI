using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record CreateCounterpartyCommand(CreateCounterpartyRequest Request) : IRequest<CounterpartyResponse>;
