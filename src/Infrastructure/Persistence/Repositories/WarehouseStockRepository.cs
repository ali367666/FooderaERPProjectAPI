using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly AppDbContext _context;

    public WarehouseStockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WarehouseStock warehouseStock, CancellationToken cancellationToken)
    {
        await _context.WarehouseStocks.AddAsync(warehouseStock, cancellationToken);
    }

    public async Task<WarehouseStock?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseStocks
            .Include(x => x.StockItem)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WarehouseStock?> GetByWarehouseAndStockItemAsync(
        int warehouseId,
        int stockItemId,
        CancellationToken cancellationToken)
    {
        return await _context.WarehouseStocks
            .FirstOrDefaultAsync(
                x => x.WarehouseId == warehouseId && x.StockItemId == stockItemId,
                cancellationToken);
    }

    public async Task<List<WarehouseStock>> GetByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken)
    {
        return await _context.WarehouseStocks
            .Include(x => x.StockItem)
            .Include(x => x.Warehouse)
            .Where(x => x.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WarehouseStock>> SearchAsync(int companyId, string? search, CancellationToken cancellationToken)
    {
        var query = _context.WarehouseStocks
            .Include(x => x.StockItem)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();

            query = query.Where(x =>
                x.StockItem.Name.ToLower().Contains(search) ||
                x.Warehouse.Name.ToLower().Contains(search) ||
                x.Id.ToString().Contains(search));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public void Delete(WarehouseStock warehouseStock)
    {
        _context.WarehouseStocks.Remove(warehouseStock);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    
}