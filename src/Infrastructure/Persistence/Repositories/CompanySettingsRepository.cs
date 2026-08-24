using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CompanySettingsRepository : ICompanySettingsRepository
{
    private readonly AppDbContext _context;

    public CompanySettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CompanySettings?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.CompanySettings
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
    }

    public async Task AddAsync(CompanySettings settings, CancellationToken cancellationToken)
    {
        await _context.CompanySettings.AddAsync(settings, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
