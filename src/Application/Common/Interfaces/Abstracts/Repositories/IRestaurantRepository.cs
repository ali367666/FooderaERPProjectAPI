using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRestaurantRepository
{
    Task AddAsync(Domain.Entities.Restaurant restaurant, CancellationToken cancellationToken);
    void Update(Domain.Entities.Restaurant restaurant);
    void Delete(Domain.Entities.Restaurant restaurant);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<Domain.Entities.Restaurant?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Restaurant>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<Domain.Entities.Restaurant>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Expression<Func<Domain.Entities.Restaurant, bool>> predicate, CancellationToken cancellationToken);
}