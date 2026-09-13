using Domain.Entities;

public interface IRestaurantSettingsRepository
{
    Task<RestaurantSettings?> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken);
    Task AddAsync(RestaurantSettings settings, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
