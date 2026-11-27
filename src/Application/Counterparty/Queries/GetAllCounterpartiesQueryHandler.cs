using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Commands;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public class GetAllCounterpartiesQueryHandler : IRequestHandler<GetAllCounterpartiesQuery, List<CounterpartyResponse>>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllCounterpartiesQueryHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<CounterpartyResponse>> Handle(GetAllCounterpartiesQuery request, CancellationToken cancellationToken)
    {
        var counterparties = await _repository.GetAllByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        var status = (request.Status ?? "active").Trim().ToLowerInvariant();
        var filtered = status switch
        {
            "all" => counterparties,
            "passive" or "inactive" => counterparties.Where(c => !c.IsActive).ToList(),
            _ => counterparties.Where(c => c.IsActive).ToList()
        };

        return filtered.Select(CreateCounterpartyCommandHandler.Map).ToList();
    }
}
