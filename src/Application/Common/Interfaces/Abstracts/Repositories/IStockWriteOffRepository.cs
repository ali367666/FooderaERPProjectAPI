using Domain.Entities.WarehouseAndStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockWriteOffRepository
{
    Task AddAsync(StockWriteOff writeOff, CancellationToken cancellationToken);

    /// <summary>Write-offs of the company between the dates (inclusive, either may be omitted), newest first.</summary>
    Task<List<StockWriteOff>> SearchAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? warehouseId, CancellationToken cancellationToken);

    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
