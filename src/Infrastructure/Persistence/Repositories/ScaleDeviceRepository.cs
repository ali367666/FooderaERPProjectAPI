using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ScaleDeviceRepository : IScaleDeviceRepository
{
    private readonly AppDbContext _context;

    public ScaleDeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ScaleDevice device, CancellationToken cancellationToken)
    {
        await _context.ScaleDevices.AddAsync(device, cancellationToken);
    }

    public async Task<ScaleDevice?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.ScaleDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<ScaleDevice>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.ScaleDevices
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.ScaleDevices
            .AnyAsync(x => x.RestaurantId == restaurantId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(ScaleDevice device) => _context.ScaleDevices.Update(device);
    public void Delete(ScaleDevice device) => _context.ScaleDevices.Remove(device);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
