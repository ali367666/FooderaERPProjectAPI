using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MenuItemTypeRepository : IMenuItemTypeRepository
{
    private readonly AppDbContext _context;

    public MenuItemTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MenuItemType itemType, CancellationToken cancellationToken)
    {
        await _context.MenuItemTypes.AddAsync(itemType, cancellationToken);
    }

    public async Task<MenuItemType?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuItemTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<MenuItemType>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.MenuItemTypes
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.MenuItemTypes
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(MenuItemType itemType) => _context.MenuItemTypes.Update(itemType);
    public void Delete(MenuItemType itemType) => _context.MenuItemTypes.Remove(itemType);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
