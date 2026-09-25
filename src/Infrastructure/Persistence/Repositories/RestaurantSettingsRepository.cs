using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantSettingsRepository : IRestaurantSettingsRepository
{
    private readonly AppDbContext _context;

    public RestaurantSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RestaurantSettings?> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantSettings
            .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId, cancellationToken);
    }

    public async Task AddAsync(RestaurantSettings settings, CancellationToken cancellationToken)
    {
        await _context.RestaurantSettings.AddAsync(settings, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
