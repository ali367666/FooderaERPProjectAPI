using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Order>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    /// <summary>Every unpaid, uncancelled order on the given tables, newest first, with the waiter and the customer.</summary>
    Task<List<Domain.Entities.Order>> GetActiveForTablesAsync(int companyId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken);

    Task<bool> HasOpenOrderForTableAsync(int tableId, int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber, int companyId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Order order);
    void Delete(Domain.Entities.Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Order>> GetPaidBetweenAsync(int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken);
}