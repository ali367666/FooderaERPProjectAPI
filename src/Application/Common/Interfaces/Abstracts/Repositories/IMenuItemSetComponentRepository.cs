namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuItemSetComponentRepository
{
    Task<List<Domain.Entities.MenuItemSetComponent>> GetBySetMenuItemIdAsync(int setMenuItemId, CancellationToken cancellationToken);
    Task AddAsync(Domain.Entities.MenuItemSetComponent component, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<Domain.Entities.MenuItemSetComponent> components);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
