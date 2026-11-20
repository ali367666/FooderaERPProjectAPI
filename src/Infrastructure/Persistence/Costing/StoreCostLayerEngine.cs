using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Costing;

/// <summary>
/// FIFO / LIFO costing of the store's warehouse goods. For a company on the weighted average (the default) it does nothing. Otherwise every
/// movement that brings goods in keeps a cost layer, and every movement that takes goods out uses the layers up (oldest first for FIFO,
/// newest first for LIFO) and gets the cost of the layers it used as its unit cost.
/// </summary>
public class StoreCostLayerEngine
{
    private readonly AppDbContext _context;
    private readonly Dictionary<int, StoreCostMethod> _methods = new();

    public StoreCostLayerEngine(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StoreCostMethod> GetMethodAsync(int companyId, CancellationToken ct)
    {
        if (_methods.TryGetValue(companyId, out var cached))
            return cached;
        var method = await _context.CompanySettings.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => (StoreCostMethod?)x.StoreCostMethod)
            .FirstOrDefaultAsync(ct) ?? StoreCostMethod.Average;
        _methods[companyId] = method;
        return method;
    }

    /// <summary>Forget the remembered method (after the SuperAdmin changed it).</summary>
    public void Forget(int companyId) => _methods.Remove(companyId);

    /// <summary>Called for every stock movement about to be saved: keeps the layers and, for goods going out, sets the movement's cost.</summary>
    public async Task ApplyAsync(StoreStockMovement movement, CancellationToken ct)
    {
        var method = await GetMethodAsync(movement.CompanyId, ct);
        if (method == StoreCostMethod.Average || movement.Quantity == 0)
            return;

        if (movement.Quantity > 0)
        {
            await _context.StoreCostLayers.AddAsync(new StoreCostLayer
            {
                CompanyId = movement.CompanyId,
                WarehouseId = movement.WarehouseId,
                StoreProductId = movement.StoreProductId,
                ReceivedAtUtc = movement.CreatedAtUtc,
                OriginalQuantity = movement.Quantity,
                RemainingQuantity = movement.Quantity,
                UnitCost = movement.UnitCost,
                DocumentType = movement.DocumentType,
                DocumentNo = movement.DocumentNo,
                CreatedByUserId = movement.CreatedByUserId,
            }, ct);
            return;
        }

        var needed = -movement.Quantity;
        var layers = await LoadLayersAsync(movement.CompanyId, movement.WarehouseId, movement.StoreProductId, method, ct);

        var cost = 0m;
        foreach (var layer in layers)
        {
            if (needed <= 0) break;
            var take = Math.Min(layer.RemainingQuantity, needed);
            cost += take * layer.UnitCost;
            layer.RemainingQuantity -= take;
            needed -= take;
        }

        // Goods that were in stock before any layer existed (or a negative-stock sale) cost what the caller worked out (the average).
        if (needed > 0)
            cost += needed * movement.UnitCost;

        movement.UnitCost = Math.Round(cost / -movement.Quantity, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>The layers of a product in a warehouse that still have goods, in the order they are used up.</summary>
    private async Task<List<StoreCostLayer>> LoadLayersAsync(int companyId, int warehouseId, int productId, StoreCostMethod method, CancellationToken ct)
    {
        var stored = await _context.StoreCostLayers
            .Where(x => x.CompanyId == companyId && x.WarehouseId == warehouseId && x.StoreProductId == productId && x.RemainingQuantity > 0)
            .ToListAsync(ct);

        // Layers made earlier in the same unit of work are not in the database yet.
        var pending = _context.StoreCostLayers.Local
            .Where(x => x.CompanyId == companyId && x.WarehouseId == warehouseId && x.StoreProductId == productId && x.RemainingQuantity > 0)
            .Except(stored)
            .ToList();

        var all = stored.Concat(pending);
        return (method == StoreCostMethod.Fifo
                ? all.OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id)
                : all.OrderByDescending(x => x.ReceivedAtUtc).ThenByDescending(x => x.Id))
            .ToList();
    }

    /// <summary>
    /// Turns FIFO / LIFO on: the stock that is there now becomes the opening layers at the product's average cost, so the first sales have a cost.
    /// Old layers (from an earlier time on FIFO / LIFO) are dropped first: the stock has moved on since.
    /// </summary>
    public async Task ResetAndSeedAsync(int companyId, CancellationToken ct)
    {
        var old = await _context.StoreCostLayers.Where(x => x.CompanyId == companyId).ToListAsync(ct);
        _context.StoreCostLayers.RemoveRange(old);

        var balances = await _context.StoreStockBalances.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.Quantity > 0)
            .Select(b => new { b.WarehouseId, b.StoreProductId, b.Quantity, Cost = b.StoreProduct.AverageCost > 0 ? b.StoreProduct.AverageCost : b.StoreProduct.PurchasePrice })
            .ToListAsync(ct);

        foreach (var b in balances)
        {
            await _context.StoreCostLayers.AddAsync(new StoreCostLayer
            {
                CompanyId = companyId,
                WarehouseId = b.WarehouseId,
                StoreProductId = b.StoreProductId,
                // Older than every real delivery: used first by FIFO, last by LIFO.
                ReceivedAtUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                OriginalQuantity = b.Quantity,
                RemainingQuantity = b.Quantity,
                UnitCost = b.Cost,
                DocumentType = "Opening",
                DocumentNo = "İlkin qalıq",
                IsOpeningBalance = true,
            }, ct);
        }
    }

    /// <summary>Removes the layers (back to the weighted average).</summary>
    public async Task DropLayersAsync(int companyId, CancellationToken ct)
    {
        var old = await _context.StoreCostLayers.Where(x => x.CompanyId == companyId).ToListAsync(ct);
        _context.StoreCostLayers.RemoveRange(old);
    }
}
