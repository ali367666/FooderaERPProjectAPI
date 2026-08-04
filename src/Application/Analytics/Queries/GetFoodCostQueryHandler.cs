using Application.Analytics.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Analytics.Queries;

public class GetFoodCostQueryHandler : IRequestHandler<GetFoodCostQuery, List<FoodCostResponse>>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetFoodCostQueryHandler(IAnalyticsRepository analyticsRepository, ICurrentUserService currentUserService)
    {
        _analyticsRepository = analyticsRepository;
        _currentUserService = currentUserService;
    }

    public Task<List<FoodCostResponse>> Handle(GetFoodCostQuery request, CancellationToken cancellationToken)
        => _analyticsRepository.GetFoodCostAsync(_currentUserService.CompanyId, cancellationToken);
}
