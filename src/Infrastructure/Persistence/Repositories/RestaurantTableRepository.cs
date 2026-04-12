using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class RestaurantTableRepository : IRestaurantTableRepository
{
    private readonly AppDbContext _context;

    public RestaurantTableRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RestaurantTable table, CancellationToken cancellationToken)
    {
        await _context.RestaurantTables.AddAsync(table, cancellationToken);
    }

    public async Task<RestaurantTable?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<RestaurantTable>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.RestaurantId)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RestaurantTable>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, int restaurantId, string name, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.RestaurantId == restaurantId &&
                x.Name == name,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, int restaurantId, int excludeId, string name, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.RestaurantId == restaurantId &&
                x.Id != excludeId &&
                x.Name == name,
                cancellationToken);
    }

    public void Update(RestaurantTable table)
    {
        _context.RestaurantTables.Update(table);
    }

    public void Delete(RestaurantTable table)
    {
        _context.RestaurantTables.Remove(table);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}