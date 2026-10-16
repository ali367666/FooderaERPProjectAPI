using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record AddCounterpartyDebtCommand(int Id, AddCounterpartyDebtRequest Request) : IRequest<CounterpartyResponse>;
