using Domain.Entities.WarehouseAndStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuItemRecipeRepository
{
    Task<List<MenuItemRecipeLine>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<List<MenuItemRecipeLine>> GetByMenuItemIdAsync(int companyId, int menuItemId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<MenuItemRecipeLine> lines, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<MenuItemRecipeLine> lines);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
