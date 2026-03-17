using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllAsync(CancellationToken cancellationToken);
    Task<Restaurant?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> AnyAsync(Expression<Func<Restaurant, bool>> predicate, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken);
    void Update(Restaurant restaurant);
    void Delete(Restaurant restaurant);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}