using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
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

    public async Task<decimal> GetStockBalanceAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken)
    {
        var movements = await _context.StockMovements
            .Where(x => x.WarehouseId == warehouseId && x.StockItemId == stockItemId)
            .ToListAsync(cancellationToken);

        return movements.Sum(x => x.Type == StockMovementType.TransferIn
            ? x.Quantity
            : -x.Quantity);
    }
}