using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

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
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.VehicleWarehouse)
            .Include(x => x.StockRequest)
            .Include(x => x.Lines)
                .ThenInclude(x => x.StockItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<WarehouseTransfer>> GetAllWithDetailsAsync(CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.VehicleWarehouse)
            .Include(x => x.StockRequest)
            .Include(x => x.Lines)
                .ThenInclude(x => x.StockItem)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Update(WarehouseTransfer warehouseTransfer)
    {
        _context.WarehouseTransfers.Update(warehouseTransfer);
    }

    public async Task DeleteAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken)
    {
        _context.WarehouseTransferLines.RemoveRange(warehouseTransfer.Lines);
        _context.WarehouseTransfers.Remove(warehouseTransfer);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}