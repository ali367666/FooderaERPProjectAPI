namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseStockRepository
{
    Task AddAsync(Domain.Entities.WarehouseAndStock.WarehouseStock warehouseStock, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.WarehouseStock?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.WarehouseStock?> GetByWarehouseAndStockItemAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.WarehouseStock>> GetByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.WarehouseStock>> SearchAsync(int companyId, string? search, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void Delete(Domain.Entities.WarehouseAndStock.WarehouseStock warehouseStock);
    void Update(Domain.Entities.WarehouseAndStock.WarehouseStock warehouseStock);

}
