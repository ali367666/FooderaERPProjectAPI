using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantSectionRepository : IRestaurantSectionRepository
{
    private readonly AppDbContext _context;

    public RestaurantSectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RestaurantSection section, CancellationToken cancellationToken)
    {
        await _context.RestaurantSections.AddAsync(section, cancellationToken);
    }

    public async Task<RestaurantSection?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantSections
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<RestaurantSection>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantSections
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantSections
            .AnyAsync(x => x.RestaurantId == restaurantId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(RestaurantSection section) => _context.RestaurantSections.Update(section);
    public void Delete(RestaurantSection section) => _context.RestaurantSections.Remove(section);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
