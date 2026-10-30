using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CompanyDepositRepository : ICompanyDepositRepository
{
    private readonly AppDbContext _context;

    public CompanyDepositRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CompanyDeposit deposit, CancellationToken cancellationToken)
        => await _context.CompanyDeposits.AddAsync(deposit, cancellationToken);

    public Task<List<CompanyDeposit>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken)
        => _context.CompanyDeposits
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOpeningAsync(int companyId, CancellationToken cancellationToken)
        => _context.CompanyDeposits.AnyAsync(x => x.CompanyId == companyId && x.IsOpening, cancellationToken);

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
