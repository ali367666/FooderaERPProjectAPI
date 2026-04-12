using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuCategoryRepository
{
    Task AddAsync(MenuCategory menuCategory, CancellationToken cancellationToken);
    Task<MenuCategory?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<MenuCategory>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, CancellationToken cancellationToken);
    void Update(MenuCategory menuCategory);
    void Delete(MenuCategory menuCategory);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByIdAsync(int id, int companyId, CancellationToken cancellationToken);
}