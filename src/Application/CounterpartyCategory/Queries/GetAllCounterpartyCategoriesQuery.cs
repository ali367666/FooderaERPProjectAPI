using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.CounterpartyCategory.Commands;
using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Queries;

public record GetAllCounterpartyCategoriesQuery : IRequest<List<CounterpartyCategoryResponse>>;

public class GetAllCounterpartyCategoriesQueryHandler : IRequestHandler<GetAllCounterpartyCategoriesQuery, List<CounterpartyCategoryResponse>>
{
    private readonly ICounterpartyCategoryRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllCounterpartyCategoriesQueryHandler(ICounterpartyCategoryRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<CounterpartyCategoryResponse>> Handle(GetAllCounterpartyCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _repository.GetAllByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        return categories.Select(CreateCounterpartyCategoryCommandHandler.Map).ToList();
    }
}
