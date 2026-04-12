using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class MenuCategoryRepository : IMenuCategoryRepository
{
    private readonly AppDbContext _context;

    public MenuCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MenuCategory menuCategory, CancellationToken cancellationToken)
    {
        await _context.MenuCategories.AddAsync(menuCategory, cancellationToken);
    }

    public async Task<MenuCategory?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<MenuCategory>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuCategories
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, CancellationToken cancellationToken)
    {
        return await _context.MenuCategories
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name, cancellationToken);
    }

    public void Update(MenuCategory menuCategory)
    {
        _context.MenuCategories.Update(menuCategory);
    }

    public void Delete(MenuCategory menuCategory)
    {
        _context.MenuCategories.Remove(menuCategory);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task<bool> ExistsByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuCategories
            .AnyAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }
}
