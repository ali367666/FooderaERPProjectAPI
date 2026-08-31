using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MenuItemSetComponentRepository : IMenuItemSetComponentRepository
{
    private readonly AppDbContext _context;

    public MenuItemSetComponentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItemSetComponent>> GetBySetMenuItemIdAsync(int setMenuItemId, CancellationToken cancellationToken)
    {
        return await _context.MenuItemSetComponents
            .Include(x => x.ComponentMenuItem)
            .Where(x => x.SetMenuItemId == setMenuItemId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MenuItemSetComponent component, CancellationToken cancellationToken)
    {
        await _context.MenuItemSetComponents.AddAsync(component, cancellationToken);
    }

    public void RemoveRange(IEnumerable<MenuItemSetComponent> components)
    {
        _context.MenuItemSetComponents.RemoveRange(components);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
