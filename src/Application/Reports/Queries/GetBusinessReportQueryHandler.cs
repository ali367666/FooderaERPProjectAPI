using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports.Dtos;
using MediatR;

namespace Application.Reports.Queries;

public class GetBusinessReportQueryHandler : IRequestHandler<GetBusinessReportQuery, BusinessReportResponse>
{
    private readonly IReportsRepository _reportsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessReportQueryHandler(IReportsRepository reportsRepository, ICurrentUserService currentUserService)
    {
        _reportsRepository = reportsRepository;
        _currentUserService = currentUserService;
    }

    public Task<BusinessReportResponse> Handle(GetBusinessReportQuery query, CancellationToken cancellationToken)
    {
        var window = ReportPeriodResolver.Resolve(query.Request);
        return _reportsRepository.GetBusinessReportAsync(
            _currentUserService.CompanyId, query.Request, window, cancellationToken);
    }
}
