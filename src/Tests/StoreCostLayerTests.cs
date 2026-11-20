using Domain.Entities;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Tests;

/// <summary>FIFO / LIFO costing: goods going out take the cost of the delivery layers they use up; the weighted average is left alone.</summary>
public class StoreCostLayerTests
{
    private const int Company = 1, Product = 1, Warehouse = 1;

    private readonly AppDbContext _db;
    private readonly StoreRepository _store;
    private readonly StoreCostingRepository _costing;

    public StoreCostLayerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new InMemoryAppDbContext(options);
        _store = new StoreRepository(_db);
        _costing = new StoreCostingRepository(_db);
    }

    private async Task UseMethodAsync(StoreCostMethod method)
    {
        _db.CompanySettings.Add(new CompanySettings { CompanyId = Company, StoreCostMethod = method });
        await _db.SaveChangesAsync();
    }

    private static StoreStockMovement In(decimal qty, decimal cost, DateTime at) => new()
    {
        CompanyId = Company, WarehouseId = Warehouse, StoreProductId = Product, Type = StoreStockMovementType.Purchase,
        Quantity = qty, UnitCost = cost, CreatedAtUtc = at, DocumentType = "Purchase", DocumentNo = "P",
    };

    private static StoreStockMovement Out(decimal qty, decimal fallbackCost) => new()
    {
        CompanyId = Company, WarehouseId = Warehouse, StoreProductId = Product, Type = StoreStockMovementType.Sale,
        Quantity = -qty, UnitCost = fallbackCost, CreatedAtUtc = DateTime.UtcNow,
    };

    private async Task<StoreStockMovement> ReceiveTwoDeliveriesThenSellAsync(decimal sold)
    {
        await _store.AddMovementAsync(In(10, 5m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)), default);
        await _store.AddMovementAsync(In(10, 8m, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)), default);
        var sale = Out(sold, 6.5m); // 6.5 = what the weighted average would say
        await _store.AddMovementAsync(sale, default);
        await _db.SaveChangesAsync();
        return sale;
    }

    [Fact]
    public async Task Fifo_takes_the_oldest_delivery_first()
    {
        await UseMethodAsync(StoreCostMethod.Fifo);
        var sale = await ReceiveTwoDeliveriesThenSellAsync(12); // 10 x 5 + 2 x 8 = 66

        Assert.Equal(5.5m, sale.UnitCost);
        var layers = await _db.StoreCostLayers.OrderBy(x => x.ReceivedAtUtc).ToListAsync();
        Assert.Equal(new[] { 0m, 8m }, layers.Select(l => l.RemainingQuantity));
    }

    [Fact]
    public async Task Lifo_takes_the_newest_delivery_first()
    {
        await UseMethodAsync(StoreCostMethod.Lifo);
        var sale = await ReceiveTwoDeliveriesThenSellAsync(12); // 10 x 8 + 2 x 5 = 90

        Assert.Equal(7.5m, sale.UnitCost);
        var layers = await _db.StoreCostLayers.OrderBy(x => x.ReceivedAtUtc).ToListAsync();
        Assert.Equal(new[] { 8m, 0m }, layers.Select(l => l.RemainingQuantity));
    }

    [Fact]
    public async Task The_weighted_average_is_left_alone_and_keeps_no_layers()
    {
        await UseMethodAsync(StoreCostMethod.Average);
        var sale = await ReceiveTwoDeliveriesThenSellAsync(12);

        Assert.Equal(6.5m, sale.UnitCost);
        Assert.Empty(_db.StoreCostLayers);
    }

    [Fact]
    public async Task Goods_without_a_layer_cost_the_fallback_average()
    {
        await UseMethodAsync(StoreCostMethod.Fifo);
        await _store.AddMovementAsync(In(2, 5m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)), default);
        var sale = Out(4, 6m); // 2 from the layer at 5, 2 with no layer at the fallback 6
        await _store.AddMovementAsync(sale, default);
        await _db.SaveChangesAsync();

        Assert.Equal(5.5m, sale.UnitCost);
    }

    [Fact]
    public async Task Switching_on_makes_an_opening_layer_from_the_stock_at_the_average_cost()
    {
        _db.StoreProducts.Add(new StoreProduct { Id = Product, CompanyId = Company, Name = "Cola", AverageCost = 4m, PurchasePrice = 3m });
        _db.StoreStockBalances.Add(new StoreStockBalance { CompanyId = Company, WarehouseId = Warehouse, StoreProductId = Product, Quantity = 7m });
        await _db.SaveChangesAsync();

        await _costing.PrepareMethodChangeAsync(Company, StoreCostMethod.Average, StoreCostMethod.Fifo, default);
        await _db.SaveChangesAsync();

        var layer = Assert.Single(_db.StoreCostLayers);
        Assert.Equal((7m, 7m, 4m, true), (layer.OriginalQuantity, layer.RemainingQuantity, layer.UnitCost, layer.IsOpeningBalance));

        // And back to the average: the layers are dropped.
        await _costing.PrepareMethodChangeAsync(Company, StoreCostMethod.Fifo, StoreCostMethod.Average, default);
        await _db.SaveChangesAsync();
        Assert.Empty(_db.StoreCostLayers);
    }
}
