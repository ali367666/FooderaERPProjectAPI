using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly AppDbContext _context;

    public RestaurantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken)
    {
        await _context.Restaurants.AddAsync(restaurant, cancellationToken);
    }

    public void Update(Restaurant restaurant)
    {
        _context.Restaurants.Update(restaurant);
    }

    public void Delete(Restaurant restaurant)
    {
        _context.Restaurants.Remove(restaurant);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Restaurant?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Restaurant>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Restaurant>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<Restaurant, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .AnyAsync(predicate, cancellationToken);
    }
}