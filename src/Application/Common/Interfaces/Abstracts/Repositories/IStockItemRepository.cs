using Application.StockItem.Dtos.Request;
using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockItemRepository
{
    Task<Domain.Entities.StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockItem>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockItem>> GetAllAsync(GetAllStockItemsRequest request, CancellationToken cancellationToken);
    Task AddAsync(Domain.Entities.StockItem stockItem, CancellationToken cancellationToken);
    void Update(Domain.Entities.StockItem stockItem);
    void Delete(Domain.Entities.StockItem stockItem);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(string barcode, int companyId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
