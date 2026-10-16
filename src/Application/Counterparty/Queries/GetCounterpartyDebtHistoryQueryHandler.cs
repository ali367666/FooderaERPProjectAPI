using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public class GetCounterpartyDebtHistoryQueryHandler
    : IRequestHandler<GetCounterpartyDebtHistoryQuery, List<CounterpartyDebtEntryResponse>>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetCounterpartyDebtHistoryQueryHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<CounterpartyDebtEntryResponse>> Handle(
        GetCounterpartyDebtHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.GetDebtEntriesAsync(request.Id, _currentUserService.CompanyId, cancellationToken);

        return entries.Select(e => new CounterpartyDebtEntryResponse
        {
            Id = e.Id,
            Type = e.Type.ToString(),
            Amount = e.Amount,
            BalanceAfter = e.BalanceAfter,
            Note = e.Note,
            OrderId = e.OrderId,
            OrderNumber = e.Order?.OrderNumber,
            CreatedAtUtc = e.CreatedAtUtc
        }).ToList();
    }
}
