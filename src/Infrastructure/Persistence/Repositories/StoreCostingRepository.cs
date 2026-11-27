using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Costing;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreCostingRepository : IStoreCostingRepository
{
    private readonly AppDbContext _context;
    private readonly StoreCostLayerEngine _engine;

    public StoreCostingRepository(AppDbContext context)
    {
        _context = context;
        _engine = new StoreCostLayerEngine(context);
    }

    public async Task PrepareMethodChangeAsync(int companyId, StoreCostMethod from, StoreCostMethod to, CancellationToken ct)
    {
        if (from == to)
            return;
        if (to == StoreCostMethod.Average)
            await _engine.DropLayersAsync(companyId, ct);
        else if (from == StoreCostMethod.Average)
            await _engine.ResetAndSeedAsync(companyId, ct);
        _engine.Forget(companyId);
    }

    public Task<StoreCostMethod> GetMethodAsync(int companyId, CancellationToken ct) => _engine.GetMethodAsync(companyId, ct);

    public async Task<List<StoreCostLayerRow>> GetLayersAsync(int companyId, int? warehouseId, int? productId, bool onlyRemaining, CancellationToken ct)
        => await _context.StoreCostLayers.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && (warehouseId == null || x.WarehouseId == warehouseId)
                && (productId == null || x.StoreProductId == productId)
                && (!onlyRemaining || x.RemainingQuantity > 0))
            .OrderBy(x => x.StoreProduct.Name).ThenBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id)
            .Take(5000)
            .Select(x => new StoreCostLayerRow(
                x.Id, x.WarehouseId, x.Warehouse.Name, x.StoreProductId, x.StoreProduct.Name, x.ReceivedAtUtc,
                x.OriginalQuantity, x.RemainingQuantity, x.UnitCost, x.DocumentType, x.DocumentNo, x.IsOpeningBalance))
            .ToListAsync(ct);

    public async Task<Dictionary<(int WarehouseId, int ProductId), decimal>?> GetStockValuesAsync(int companyId, CancellationToken ct)
    {
        if (await _engine.GetMethodAsync(companyId, ct) == StoreCostMethod.Average)
            return null;

        var layers = await _context.StoreCostLayers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.RemainingQuantity > 0)
            .GroupBy(x => new { x.WarehouseId, x.StoreProductId })
            .Select(g => new { g.Key.WarehouseId, g.Key.StoreProductId, Qty = g.Sum(x => x.RemainingQuantity), Value = g.Sum(x => x.RemainingQuantity * x.UnitCost) })
            .ToListAsync(ct);
        var balances = await _context.StoreStockBalances.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.Quantity != 0)
            .Select(b => new { b.WarehouseId, b.StoreProductId, b.Quantity, Cost = b.StoreProduct.AverageCost > 0 ? b.StoreProduct.AverageCost : b.StoreProduct.PurchasePrice })
            .ToListAsync(ct);

        var byKey = layers.ToDictionary(l => (l.WarehouseId, l.StoreProductId));
        var result = new Dictionary<(int WarehouseId, int ProductId), decimal>();
        foreach (var b in balances)
        {
            // The layers' value, plus the part of the stock without a layer at the average cost.
            byKey.TryGetValue((b.WarehouseId, b.StoreProductId), out var l);
            var value = (l?.Value ?? 0m) + Math.Max(0m, b.Quantity - (l?.Qty ?? 0m)) * b.Cost;
            result[(b.WarehouseId, b.StoreProductId)] = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
        return result;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
