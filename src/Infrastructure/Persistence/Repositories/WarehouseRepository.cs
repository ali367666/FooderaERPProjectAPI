using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .Include(x => x.Restaurant)
            .Include(x => x.DriverUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Warehouse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .Include(x => x.Restaurant)
            .Include(x => x.DriverUser)
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Warehouse>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .Include(x => x.Restaurant)
            .Include(x => x.DriverUser)
            .Where(x => x.CompanyId == companyId)
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Warehouse>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .Include(x => x.Restaurant)
            .Include(x => x.DriverUser)
            .Where(x => x.RestaurantId == restaurantId)
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Warehouse>> GetByDriverUserIdAsync(int driverUserId, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .Include(x => x.Restaurant)
            .Include(x => x.DriverUser)
            .Where(x => x.DriverUserId == driverUserId)
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int companyId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLower();

        return await _context.Warehouses
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.Name.ToLower() == normalizedName,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int companyId, int excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLower();

        return await _context.Warehouses
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.Id != excludeId &&
                x.Name.ToLower() == normalizedName,
                cancellationToken);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken)
    {
        await _context.Warehouses.AddAsync(warehouse, cancellationToken);
    }

    public void Update(Warehouse warehouse)
    {
        _context.Warehouses.Update(warehouse);
    }

    public void Delete(Warehouse warehouse)
    {
        _context.Warehouses.Remove(warehouse);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken)
    {
        _context.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }
    public async Task<List<Warehouse>> SearchAsync(
    int companyId,
    string? search,
    CancellationToken cancellationToken)
    {
        var query = _context.Warehouses
            .Where(x => x.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();

            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Id.ToString().Contains(search));
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}