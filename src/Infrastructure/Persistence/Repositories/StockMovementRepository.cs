using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockMovement stockMovement, CancellationToken cancellationToken)
    {
        await _context.StockMovements.AddAsync(stockMovement, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<StockMovement> stockMovements, CancellationToken cancellationToken)
    {
        await _context.StockMovements.AddRangeAsync(stockMovements, cancellationToken);
    }

    public async Task<List<StockMovement>> GetByWarehouseTransferIdAsync(int warehouseTransferId, CancellationToken cancellationToken)
    {
        return await _context.StockMovements
            .Where(x => x.WarehouseTransferId == warehouseTransferId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockMovement>> SearchByCompanyAsync(
        int companyId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Include(x => x.StockItem)
            .Include(x => x.Warehouse)
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.SourceDocumentNo != null && x.SourceDocumentNo.ToLower().Contains(s)) ||
                x.StockItem.Name.ToLower().Contains(s) ||
                x.Warehouse.Name.ToLower().Contains(s) ||
                (x.FromWarehouse != null && x.FromWarehouse.Name.ToLower().Contains(s)) ||
                (x.ToWarehouse != null && x.ToWarehouse.Name.ToLower().Contains(s)) ||
                (x.Note != null && x.Note.ToLower().Contains(s)));
        }

        return await query
            .OrderByDescending(x => x.MovementDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetStockBalanceAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken)
    {
        var row = await _context.WarehouseStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.WarehouseId == warehouseId && x.StockItemId == stockItemId,
                cancellationToken);

        return row?.Quantity ?? 0;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}