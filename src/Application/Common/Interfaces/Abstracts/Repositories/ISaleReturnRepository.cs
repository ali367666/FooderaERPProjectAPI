namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ISaleReturnRepository
{
    Task AddAsync(Domain.Entities.SaleReturn saleReturn, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> FindPaidOrderForReturnAsync(int companyId, string code, CancellationToken cancellationToken);
    Task<List<Domain.Entities.SaleReturn>> GetByOrderIdAsync(int companyId, int orderId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.SaleReturn>> GetBetweenAsync(
        int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
