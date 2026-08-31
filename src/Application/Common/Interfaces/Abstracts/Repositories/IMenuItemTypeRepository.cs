namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuItemTypeRepository
{
    Task AddAsync(Domain.Entities.MenuItemType itemType, CancellationToken cancellationToken);
    Task<Domain.Entities.MenuItemType?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.MenuItemType>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.MenuItemType itemType);
    void Delete(Domain.Entities.MenuItemType itemType);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
