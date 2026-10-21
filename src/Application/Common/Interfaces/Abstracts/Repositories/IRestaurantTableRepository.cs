using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRestaurantTableRepository
{
    Task AddAsync(Domain.Entities.RestaurantTable table, CancellationToken cancellationToken);
    Task<Domain.Entities.RestaurantTable?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.RestaurantTable>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.RestaurantTable>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    /// <summary>The company's tables (optionally one restaurant's) with their section, for the POS tables search.</summary>
    Task<List<Domain.Entities.RestaurantTable>> GetForSearchAsync(int companyId, int? restaurantId, CancellationToken cancellationToken);

    Task<bool> RestaurantExistsAsync(int companyId, int restaurantId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(int companyId, int restaurantId, string name, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, int restaurantId, int excludeId, string name, CancellationToken cancellationToken);

    void Update(Domain.Entities.RestaurantTable table);
    void Delete(Domain.Entities.RestaurantTable table);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}