using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FiscalDeviceRepository : IFiscalDeviceRepository
{
    private readonly AppDbContext _context;

    public FiscalDeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FiscalDevice device, CancellationToken cancellationToken)
    {
        await _context.FiscalDevices.AddAsync(device, cancellationToken);
    }

    public async Task<FiscalDevice?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.FiscalDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<FiscalDevice>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.FiscalDevices
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.FiscalDevices
            .AnyAsync(x => x.RestaurantId == restaurantId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(FiscalDevice device) => _context.FiscalDevices.Update(device);
    public void Delete(FiscalDevice device) => _context.FiscalDevices.Remove(device);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
