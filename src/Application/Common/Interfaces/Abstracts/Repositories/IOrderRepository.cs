using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Order>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> HasOpenOrderForTableAsync(int tableId, int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber, int companyId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Order order);
    void Delete(Domain.Entities.Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
}