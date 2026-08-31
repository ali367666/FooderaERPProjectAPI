using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Commands;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public record GetAllCounterpartiesQuery : IRequest<List<CounterpartyResponse>>;

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
        return counterparties.Select(CreateCounterpartyCommandHandler.Map).ToList();
    }
}
