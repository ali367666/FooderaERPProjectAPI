using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports.Dtos;
using MediatR;

namespace Application.Reports.Queries;

public class GetBusinessReportQueryHandler : IRequestHandler<GetBusinessReportQuery, BusinessReportResponse>
{
    private readonly IReportsRepository _reportsRepository;
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessReportQueryHandler(
        IReportsRepository reportsRepository, ICompanySettingsRepository settingsRepository, ICurrentUserService currentUserService)
    {
        _settingsRepository = settingsRepository;
        _reportsRepository = reportsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BusinessReportResponse> Handle(GetBusinessReportQuery query, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);
        var window = ReportPeriodResolver.Resolve(query.Request, BusinessTime.Now, ReportPeriodResolver.DayStartFor(settings));
        return await _reportsRepository.GetBusinessReportAsync(
            _currentUserService.CompanyId, query.Request, window, cancellationToken);
    }
}
