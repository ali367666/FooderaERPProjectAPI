using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly AppDbContext _context;
    public DiscountRepository(AppDbContext context) => _context = context;

    public Task<List<Discount>> GetAllAsync(int companyId, CancellationToken ct)
        => _context.Discounts.Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.Id).ToListAsync(ct);

    public Task<Discount?> GetByIdAsync(int id, int companyId, CancellationToken ct)
        => _context.Discounts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<Discount?> GetByCodeAsync(string code, int companyId, CancellationToken ct)
        => _context.Discounts.FirstOrDefaultAsync(x => x.Code == code && x.CompanyId == companyId, ct);

    public async Task<HashSet<int>> GetDiscountExcludedMenuItemIdsAsync(int companyId, IReadOnlyCollection<int> menuItemIds, CancellationToken ct)
    {
        if (menuItemIds.Count == 0) return new();

        var items = await _context.MenuItems.AsNoTracking()
            .Where(x => menuItemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ExcludeFromDiscount, x.MenuCategoryId })
            .ToListAsync(ct);
        var categories = await _context.MenuCategories.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => new { x.Id, x.ParentCategoryId, x.ExcludeFromDiscount })
            .ToDictionaryAsync(x => x.Id, ct);

        bool CategoryExcluded(int? id)
        {
            // Walk up the parents (the depth guard only protects against a bad cycle in the data).
            for (var depth = 0; id is { } current && depth < 20 && categories.TryGetValue(current, out var category); depth++)
            {
                if (category.ExcludeFromDiscount) return true;
                id = category.ParentCategoryId;
            }
            return false;
        }

        return items.Where(x => x.ExcludeFromDiscount || CategoryExcluded(x.MenuCategoryId)).Select(x => x.Id).ToHashSet();
    }

    public Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId, CancellationToken ct)
        => _context.Discounts.AnyAsync(x =>
            x.Code == code && x.CompanyId == companyId && (excludeId == null || x.Id != excludeId), ct);

    public async Task AddAsync(Discount discount, CancellationToken ct)
        => await _context.Discounts.AddAsync(discount, ct);

    public void Update(Discount discount) => _context.Discounts.Update(discount);
    public void Remove(Discount discount) => _context.Discounts.Remove(discount);
    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
