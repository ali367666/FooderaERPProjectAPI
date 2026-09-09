using Application.Analytics.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Analytics.Queries;

public record GetSalesReportQuery(DateTime From, DateTime To) : IRequest<SalesReportResponse>;

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportResponse>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSalesReportQueryHandler(IAnalyticsRepository analyticsRepository, ICurrentUserService currentUserService)
    {
        _analyticsRepository = analyticsRepository;
        _currentUserService = currentUserService;
    }

    public Task<SalesReportResponse> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        return _analyticsRepository.GetSalesReportAsync(
            _currentUserService.CompanyId,
            request.From,
            request.To,
            cancellationToken);
    }
}
