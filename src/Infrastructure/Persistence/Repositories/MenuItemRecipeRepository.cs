using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class MenuItemRecipeRepository : IMenuItemRecipeRepository
{
    private readonly AppDbContext _context;

    public MenuItemRecipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItemRecipeLine>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        var baseQuery = _context.MenuItemRecipeLines
            .AsNoTracking()
            .Include(x => x.MenuItem)
                .ThenInclude(x => x.MenuCategory)
            .Include(x => x.StockItem)
            .AsQueryable();

        // Primary company filter by MenuItem.CompanyId.
        var filtered = baseQuery.Where(x => x.MenuItem.CompanyId == companyId);
        var data = await filtered
            .OrderBy(x => x.MenuItem.Name)
            .ThenBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);

        if (data.Count > 0)
            return data;

        // Fallback path if MenuItem.CompanyId is missing/not populated:
        var fallback = await baseQuery
            .Where(x => x.MenuItem.MenuCategory.CompanyId == companyId)
            .OrderBy(x => x.MenuItem.Name)
            .ThenBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);

        if (fallback.Count > 0)
            return fallback;

        // Temporary diagnostic fallback to confirm DB data visibility.
        return await baseQuery
            .OrderBy(x => x.MenuItem.Name)
            .ThenBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MenuItemRecipeLine>> GetByMenuItemIdAsync(
        int companyId,
        int menuItemId,
        CancellationToken cancellationToken)
    {
        var baseQuery = _context.MenuItemRecipeLines
            .Include(x => x.MenuItem)
                .ThenInclude(x => x.MenuCategory)
            .Include(x => x.StockItem)
            .Where(x => x.MenuItemId == menuItemId)
            .AsQueryable();

        var data = await baseQuery
            .Where(x => x.MenuItem.CompanyId == companyId)
            .OrderBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);
        if (data.Count > 0)
            return data;

        var fallback = await baseQuery
            .Where(x => x.MenuItem.MenuCategory.CompanyId == companyId)
            .OrderBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);
        if (fallback.Count > 0)
            return fallback;

        return await baseQuery
            .OrderBy(x => x.StockItem.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<MenuItemRecipeLine> lines, CancellationToken cancellationToken)
    {
        await _context.MenuItemRecipeLines.AddRangeAsync(lines, cancellationToken);
    }

    public void RemoveRange(IEnumerable<MenuItemRecipeLine> lines)
    {
        _context.MenuItemRecipeLines.RemoveRange(lines);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
