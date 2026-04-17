using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Persistence.Repositories;

public class WarehouseTransferRepository : IWarehouseTransferRepository
{
    private readonly AppDbContext _context;

    public WarehouseTransferRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken)
    {
        await _context.WarehouseTransfers.AddAsync(warehouseTransfer, cancellationToken);
    }

    public async Task<WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Include(x => x.Lines)
                .ThenInclude(x => x.StockItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<WarehouseTransfer>> GetAllWithDetailsAsync(CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);
    }

    public void Update(WarehouseTransfer warehouseTransfer)
    {
        _context.WarehouseTransfers.Update(warehouseTransfer);
    }

    public async Task DeleteAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken)
    {
        _context.WarehouseTransfers.Remove(warehouseTransfer);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> WarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AnyAsync(x => x.Id == warehouseId, cancellationToken);
    }

    public async Task<List<int>> GetExistingStockItemIdsAsync(List<int> stockItemIds, CancellationToken cancellationToken)
    {
        return await _context.StockItems
            .Where(x => stockItemIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void RemoveLines(IEnumerable<WarehouseTransferLine> lines)
    {
        _context.WarehouseTransferLines.RemoveRange(lines);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }
}