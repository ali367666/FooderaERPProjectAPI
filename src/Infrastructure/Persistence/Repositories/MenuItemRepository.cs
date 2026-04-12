using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly AppDbContext _context;

    public MenuItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        await _context.MenuItems.AddAsync(menuItem, cancellationToken);
    }

    public async Task<MenuItem?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuItems
            .Include(x => x.MenuCategory)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<MenuItem>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuItems
            .Include(x => x.MenuCategory)
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, int categoryId, string name, CancellationToken cancellationToken)
    {
        name = name.Trim().ToLower();

        return await _context.MenuItems
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.MenuCategoryId == categoryId &&
                x.Name.ToLower() == name,
                cancellationToken);
    }

    public void Update(MenuItem menuItem)
    {
        _context.MenuItems.Update(menuItem);
    }

    public void Delete(MenuItem menuItem)
    {
        _context.MenuItems.Remove(menuItem);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
    
}