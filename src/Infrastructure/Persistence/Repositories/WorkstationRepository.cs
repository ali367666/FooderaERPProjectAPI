using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class WorkstationRepository : IWorkstationRepository
{
    private readonly AppDbContext _context;

    public WorkstationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Workstation workstation, CancellationToken cancellationToken)
    {
        await _context.Workstations.AddAsync(workstation, cancellationToken);
    }

    public async Task<Workstation?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Workstations
            .Include(x => x.Restaurant)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Workstation>> GetAllByCompanyAsync(int companyId, int? restaurantId, CancellationToken cancellationToken)
    {
        return await _context.Workstations
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .Where(x => x.CompanyId == companyId && (restaurantId == null || x.RestaurantId == restaurantId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Workstation>> GetActiveByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Workstations
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.Workstations
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public async Task TouchLastSeenAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        await _context.Workstations
            .Where(x => x.Id == id && x.CompanyId == companyId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAtUtc, DateTime.UtcNow), cancellationToken);
    }

    public void Update(Workstation workstation) => _context.Workstations.Update(workstation);
    public void Delete(Workstation workstation) => _context.Workstations.Remove(workstation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
