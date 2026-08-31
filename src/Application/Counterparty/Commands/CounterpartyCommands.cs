using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record CreateCounterpartyCommand(CreateCounterpartyRequest Request) : IRequest<CounterpartyResponse>;

public record UpdateCounterpartyCommand(UpdateCounterpartyRequest Request) : IRequest<CounterpartyResponse>;

public record DeleteCounterpartyCommand(int Id) : IRequest;

public record AdjustCounterpartyDebtCommand(int Id, AdjustCounterpartyDebtRequest Request) : IRequest<CounterpartyResponse>;
