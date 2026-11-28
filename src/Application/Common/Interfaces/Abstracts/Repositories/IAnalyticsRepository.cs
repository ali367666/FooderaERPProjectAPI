using Application.Analytics.Dtos;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IAnalyticsRepository
{
    Task<DashboardAnalyticsResponse> GetDashboardAsync(int companyId, CancellationToken cancellationToken);
    Task<List<FoodCostResponse>> GetFoodCostAsync(int companyId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetSalesReportAsync(int companyId, DateTime from, DateTime to, CancellationToken cancellationToken);
    /// <summary>Fills what the report page compares and draws: payment split, earlier period, days, waiters and the floor tables.</summary>
    Task AddSalesInsightsAsync(int companyId, SalesReportResponse report, TimeSpan dayStart, CancellationToken cancellationToken);
}
