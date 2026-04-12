using Application.StockItem.Dtos.Request;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockItemRepository
{
    Task<Domain.Entities.WarehouseAndStock.StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockItem>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockItem>> GetAllAsync(GetAllStockItemsRequest request, CancellationToken cancellationToken);
    Task AddAsync(Domain.Entities.WarehouseAndStock.StockItem stockItem, CancellationToken cancellationToken);
    void Update(Domain.Entities.WarehouseAndStock.StockItem stockItem);
    void Delete(Domain.Entities.WarehouseAndStock.StockItem stockItem);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(string barcode, int companyId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
