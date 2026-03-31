using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseStockRepository
{
    Task AddAsync(Domain.Entities.WarehouseStock warehouseStock, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseStock?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseStock?> GetByWarehouseAndStockItemAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseStock>> GetByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseStock>> SearchAsync(int companyId, string? search, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void Delete(Domain.Entities.WarehouseStock warehouseStock);
    
}
