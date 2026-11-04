using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Xunit;

namespace Tests;

public class DiscountExclusionTests
{
    private const int Company = 1;

    private static (AppDbContext Db, DiscountRepository Repo) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        return (db, new DiscountRepository(db));
    }

    [Fact]
    public async Task A_marked_product_a_marked_category_and_a_marked_parent_category_are_all_excluded()
    {
        var (db, repo) = Create();
        db.MenuCategories.AddRange(
            new MenuCategory { Id = 1, CompanyId = Company, Name = "Normal" },
            new MenuCategory { Id = 2, CompanyId = Company, Name = "Alkoqol", ExcludeFromDiscount = true },
            new MenuCategory { Id = 3, CompanyId = Company, Name = "Pivə", ParentCategoryId = 2 });
        db.MenuItems.AddRange(
            new MenuItem { Id = 10, CompanyId = Company, Name = "Plov", MenuCategoryId = 1 },
            new MenuItem { Id = 11, CompanyId = Company, Name = "Xüsusi", MenuCategoryId = 1, ExcludeFromDiscount = true },
            new MenuItem { Id = 12, CompanyId = Company, Name = "Vino", MenuCategoryId = 2 },
            new MenuItem { Id = 13, CompanyId = Company, Name = "Qara pivə", MenuCategoryId = 3 });
        await db.SaveChangesAsync();

        var excluded = await repo.GetDiscountExcludedMenuItemIdsAsync(Company, new[] { 10, 11, 12, 13 }, default);

        Assert.DoesNotContain(10, excluded);
        Assert.Contains(11, excluded);
        Assert.Contains(12, excluded);
        Assert.Contains(13, excluded);
    }

    [Fact]
    public async Task No_items_means_nothing_is_excluded()
    {
        var (_, repo) = Create();

        Assert.Empty(await repo.GetDiscountExcludedMenuItemIdsAsync(Company, Array.Empty<int>(), default));
    }
}
