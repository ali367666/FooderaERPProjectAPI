using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public record GetCounterpartyDebtHistoryQuery(int Id) : IRequest<List<CounterpartyDebtEntryResponse>>;
