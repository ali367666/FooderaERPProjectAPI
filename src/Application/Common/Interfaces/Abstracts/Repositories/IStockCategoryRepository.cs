using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockCategoryRepository
{
    Task<Domain.Entities.StockCategory?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.StockCategory?> GetByIdWithChildrenAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetRootCategoriesByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetChildrenByParentIdAsync(int parentId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, int companyId, int excludeId, CancellationToken cancellationToken);
    Task<bool> BelongsToCompanyAsync(int categoryId, int companyId, CancellationToken cancellationToken);
    Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(Domain.Entities.StockCategory stockCategory, CancellationToken cancellationToken);
    void Update(Domain.Entities.StockCategory stockCategory);
    void Delete(Domain.Entities.StockCategory stockCategory);

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockCategory>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
}