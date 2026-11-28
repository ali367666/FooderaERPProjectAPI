using Application.Analytics.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using MediatR;

namespace Application.Analytics.Queries;

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportResponse>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ICurrentUserService _currentUserService;

    private readonly ICompanySettingsRepository _settingsRepository;

    public GetSalesReportQueryHandler(
        IAnalyticsRepository analyticsRepository, ICompanySettingsRepository settingsRepository, ICurrentUserService currentUserService)
    {
        _settingsRepository = settingsRepository;
        _analyticsRepository = analyticsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<SalesReportResponse> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var report = await _analyticsRepository.GetSalesReportAsync(companyId, request.From, request.To, cancellationToken);

        var dayStart = ReportPeriodResolver.DayStartFor(await _settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken));
        await _analyticsRepository.AddSalesInsightsAsync(companyId, report, dayStart, cancellationToken);
        return report;
    }
}
