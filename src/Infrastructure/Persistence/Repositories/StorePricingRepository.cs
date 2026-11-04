using Application.StoreMode.Pricing;
using Domain.Entities.StoreMode;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StorePricingRepository : IStorePricingRepository
{
    private readonly AppDbContext _context;

    public StorePricingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Dictionary<int, decimal>> GetBranchPricesAsync(int companyId, int branchId, IReadOnlyCollection<int> productIds, CancellationToken ct)
        => _context.StoreBranchPrices.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.RestaurantId == branchId && productIds.Contains(x.StoreProductId))
            .ToDictionaryAsync(x => x.StoreProductId, x => x.SalePrice, ct);

    public async Task<List<(int BranchId, decimal SalePrice)>> GetProductBranchPricesAsync(int companyId, int productId, CancellationToken ct)
        => (await _context.StoreBranchPrices.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StoreProductId == productId)
            .Select(x => new { x.RestaurantId, x.SalePrice }).ToListAsync(ct))
            .Select(x => (x.RestaurantId, x.SalePrice)).ToList();

    public async Task ReplaceProductBranchPricesAsync(int companyId, int productId, IReadOnlyCollection<(int BranchId, decimal SalePrice)> prices, CancellationToken ct)
    {
        var current = await _context.StoreBranchPrices.Where(x => x.CompanyId == companyId && x.StoreProductId == productId).ToListAsync(ct);
        _context.StoreBranchPrices.RemoveRange(current.Where(x => prices.All(p => p.BranchId != x.RestaurantId)));
        foreach (var (branchId, price) in prices)
        {
            var row = current.FirstOrDefault(x => x.RestaurantId == branchId);
            if (row is null)
                _context.StoreBranchPrices.Add(new StoreBranchPrice { CompanyId = companyId, StoreProductId = productId, RestaurantId = branchId, SalePrice = price });
            else
                row.SalePrice = price;
        }
        await _context.SaveChangesAsync(ct);
    }
}
