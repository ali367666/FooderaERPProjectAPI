using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuItemRepository
{
    Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken);
    Task<MenuItem?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    /// <summary>Several items (with their set components) in one query — e.g. every product of one quick sale.</summary>
    Task<List<MenuItem>> GetByIdsAsync(IReadOnlyCollection<int> ids, int companyId, CancellationToken cancellationToken);
    Task<List<MenuItem>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, int categoryId, string name, CancellationToken cancellationToken);
    Task<bool> ExistsByItemTypeIdAsync(int itemTypeId, CancellationToken cancellationToken);
    void Update(MenuItem menuItem);
    void Delete(MenuItem menuItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
}