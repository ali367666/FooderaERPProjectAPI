using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRestaurantRepository
{
    Task<List<Domain.Entities.Restaurant>> GetAllAsync(CancellationToken cancellationToken);
    Task<Domain.Entities.Restaurant?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> AnyAsync(Expression<Func<Domain.Entities.Restaurant, bool>> predicate, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Domain.Entities.Restaurant restaurant, CancellationToken cancellationToken);
    void Update(Domain.Entities.Restaurant restaurant);
    void Delete(Domain.Entities.Restaurant restaurant);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}