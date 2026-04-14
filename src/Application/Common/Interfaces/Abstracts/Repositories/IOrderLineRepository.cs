using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderLineRepository
{
    Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken);
    Task<OrderLine?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<OrderLine>> GetByOrderIdAsync(int orderId, int companyId, CancellationToken cancellationToken);
    void Update(OrderLine orderLine);
    void Delete(OrderLine orderLine);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<List<OrderLine>> GetKitchenLinesAsync(
    int companyId,
    int restaurantId,
    CancellationToken cancellationToken);

    Task<List<OrderLine>> GetReadyKitchenLinesAsync(
        int companyId,
        int restaurantId,
        CancellationToken cancellationToken);
}