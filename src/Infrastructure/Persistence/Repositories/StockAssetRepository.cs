using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StockAssetRepository : IStockAssetRepository
{
    private readonly AppDbContext _context;

    public StockAssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<StockItem>> GetAssetItemsAsync(int companyId, bool allTypes, CancellationToken cancellationToken)
        => _context.StockItems.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.WarehouseStocks).ThenInclude(w => w.Warehouse)
            .Where(x => x.CompanyId == companyId && (allTypes || x.Type == StockItemType.NonFood))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<List<StockMovement>> GetAssetMovementsAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? stockItemId, CancellationToken cancellationToken)
        => _context.StockMovements.AsNoTracking()
            .Include(x => x.StockItem)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId
                && (stockItemId != null ? x.StockItemId == stockItemId : x.StockItem.Type == StockItemType.NonFood)
                && (fromUtc == null || x.MovementDate >= fromUtc)
                && (toUtcExclusive == null || x.MovementDate < toUtcExclusive))
            .OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Id)
            .Take(2000)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<int, StockWriteOffReason>> GetWriteOffReasonsAsync(IEnumerable<int> writeOffIds, CancellationToken cancellationToken)
    {
        var ids = writeOffIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, StockWriteOffReason>();
        return await _context.StockWriteOffs.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Reason, cancellationToken);
    }
}
