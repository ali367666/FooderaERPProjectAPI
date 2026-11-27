using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

/// <summary>First a product is entered by hand; after that scanning its barcode fills the form for every company.</summary>
public class BarcodeCatalogTests
{
    private static (AppDbContext Db, BarcodeCatalog Catalog) Create()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return (db, new BarcodeCatalog(db));
    }

    [Fact]
    public async Task An_unknown_barcode_finds_nothing()
    {
        var (_, catalog) = Create();

        Assert.Null(await catalog.LookupAsync(1, "4780000000011", default));
    }

    [Fact]
    public async Task A_product_entered_by_hand_is_found_by_another_company()
    {
        var (_, catalog) = Create();
        await catalog.RememberAsync("5449000000996", "Coca-Cola 0.5 L", 1, 2, default);

        var info = await catalog.LookupAsync(2, "5449000000996", default);

        Assert.NotNull(info);
        Assert.Equal("catalog", info!.Source);
        Assert.Equal("Coca-Cola 0.5 L", info.Name);
        Assert.Equal(1, info.Type);
        Assert.Equal(2, info.Unit);
        Assert.False(info.ExistsInCompany);
    }

    [Fact]
    public async Task The_first_entry_stays()
    {
        var (_, catalog) = Create();
        await catalog.RememberAsync("123456", "Birinci ad", 1, 1, default);
        await catalog.RememberAsync("123456", "İkinci ad", 2, 2, default);

        Assert.Equal("Birinci ad", (await catalog.LookupAsync(1, "123456", default))!.Name);
    }

    [Fact]
    public async Task A_company_that_already_has_the_item_is_told_so()
    {
        var (db, catalog) = Create();
        db.StockItems.Add(new StockItem { Id = 9, CompanyId = 1, Name = "Su", Barcode = "999", Type = StockItemType.FinishedGood, Unit = UnitOfMeasure.Piece });
        await db.SaveChangesAsync();

        var info = await catalog.LookupAsync(1, "999", default);

        Assert.True(info!.ExistsInCompany);
        Assert.Equal(9, info.ExistingStockItemId);
    }

    [Fact]
    public async Task Another_companys_older_item_teaches_the_barcode_without_a_catalog_entry()
    {
        var (db, catalog) = Create();
        db.StockItems.Add(new StockItem { Id = 1, CompanyId = 7, Name = "Fanta", Barcode = "888", Type = StockItemType.FinishedGood, Unit = UnitOfMeasure.Piece });
        await db.SaveChangesAsync();

        var info = await catalog.LookupAsync(2, "888", default);

        Assert.Equal("Fanta", info!.Name);
        Assert.False(info.ExistsInCompany);
    }
}
