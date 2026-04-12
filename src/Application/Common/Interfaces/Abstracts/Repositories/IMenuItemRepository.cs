using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IMenuItemRepository
{
    Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken);
    Task<MenuItem?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<MenuItem>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, int categoryId, string name, CancellationToken cancellationToken);
    void Update(MenuItem menuItem);
    void Delete(MenuItem menuItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
}