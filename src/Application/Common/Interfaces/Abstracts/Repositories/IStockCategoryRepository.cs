namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockCategoryRepository
{
    Task<Domain.Entities.WarehouseAndStock.StockCategory?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.StockCategory?> GetByIdWithChildrenAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetRootCategoriesByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetChildrenByParentIdAsync(int parentId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, int companyId, int excludeId, CancellationToken cancellationToken);
    Task<bool> BelongsToCompanyAsync(int categoryId, int companyId, CancellationToken cancellationToken);
    Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(Domain.Entities.WarehouseAndStock.StockCategory stockCategory, CancellationToken cancellationToken);
    void Update(Domain.Entities.WarehouseAndStock.StockCategory stockCategory);
    void Delete(Domain.Entities.WarehouseAndStock.StockCategory stockCategory);

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockCategory>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
}