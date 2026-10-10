using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record AdjustCounterpartyDebtCommand(int Id, AdjustCounterpartyDebtRequest Request) : IRequest<CounterpartyResponse>;
