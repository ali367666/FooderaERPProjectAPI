using Application.Analytics.Dtos;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IAnalyticsRepository
{
    Task<DashboardAnalyticsResponse> GetDashboardAsync(int companyId, CancellationToken cancellationToken);
    Task<List<FoodCostResponse>> GetFoodCostAsync(int companyId, CancellationToken cancellationToken);
}
