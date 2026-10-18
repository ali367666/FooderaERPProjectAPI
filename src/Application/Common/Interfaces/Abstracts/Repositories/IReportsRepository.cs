using Application.Reports.Dtos;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IReportsRepository
{
    Task<BusinessReportResponse> GetBusinessReportAsync(
        int companyId,
        BusinessReportRequest request,
        ReportWindow window,
        CancellationToken cancellationToken);
}
