using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly AppDbContext _context;

    public WarehouseStockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseStock?> GetByWarehouseAndStockItemAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        CancellationToken cancellationToken)
    {
        return await _context.WarehouseStocks
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId
                     && x.WarehouseId == warehouseId
                     && x.StockItemId == stockItemId,
                cancellationToken);
    }

    public async Task<List<WarehouseStock>> SearchAsync(
        int companyId,
        int? warehouseId,
        int? stockItemId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.WarehouseStocks
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.StockItem)
            .Where(x => x.CompanyId == companyId);

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (stockItemId.HasValue)
            query = query.Where(x => x.StockItemId == stockItemId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.Warehouse.Name.ToLower().Contains(s) ||
                x.StockItem.Name.ToLower().Contains(s));
        }

        return await query
            .OrderBy(x => x.Warehouse.Name)
            .ThenBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WarehouseStock warehouseStock, CancellationToken cancellationToken)
    {
        await _context.WarehouseStocks.AddAsync(warehouseStock, cancellationToken);
    }

    public void Update(WarehouseStock warehouseStock)
    {
        _context.WarehouseStocks.Update(warehouseStock);
    }

    public async Task<WarehouseStock> GetOrCreateZeroBalanceAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        int unitId,
        int? createdByUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var row = await GetByWarehouseAndStockItemAsync(
            companyId,
            warehouseId,
            stockItemId,
            cancellationToken);

        if (row is not null)
            return row;

        row = new WarehouseStock
        {
            CompanyId = companyId,
            WarehouseId = warehouseId,
            StockItemId = stockItemId,
            Quantity = 0,
            UnitId = unitId,
            CreatedAtUtc = utcNow,
            CreatedByUserId = createdByUserId,
        };

        await AddAsync(row, cancellationToken);
        return row;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
