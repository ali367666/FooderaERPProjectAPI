using Domain.Entities.WarehouseAndStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseStockDocumentRepository
{
    Task AddAsync(WarehouseStockDocument document, CancellationToken cancellationToken);

    Task<WarehouseStockDocument?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);

    Task<List<WarehouseStockDocument>> SearchByCompanyAsync(int companyId, string? search, CancellationToken cancellationToken);

    Task<List<WarehouseStockDocument>> GetByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken);

    void Update(WarehouseStockDocument document);

    void Remove(WarehouseStockDocument document);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
