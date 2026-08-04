using Application.Analytics.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Analytics.Queries;

public class GetDashboardAnalyticsQueryHandler
    : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsResponse>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardAnalyticsQueryHandler(
        IAnalyticsRepository analyticsRepository,
        ICurrentUserService currentUserService)
    {
        _analyticsRepository = analyticsRepository;
        _currentUserService = currentUserService;
    }

    public Task<DashboardAnalyticsResponse> Handle(
        GetDashboardAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        return _analyticsRepository.GetDashboardAsync(
            _currentUserService.CompanyId,
            cancellationToken);
    }
}
