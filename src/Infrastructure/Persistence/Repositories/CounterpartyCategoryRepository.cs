using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CounterpartyCategoryRepository : ICounterpartyCategoryRepository
{
    private readonly AppDbContext _context;

    public CounterpartyCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CounterpartyCategory category, CancellationToken cancellationToken)
    {
        await _context.CounterpartyCategories.AddAsync(category, cancellationToken);
    }

    public async Task<CounterpartyCategory?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.CounterpartyCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<CounterpartyCategory>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.CounterpartyCategories
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.CounterpartyCategories
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(CounterpartyCategory category) => _context.CounterpartyCategories.Update(category);
    public void Delete(CounterpartyCategory category) => _context.CounterpartyCategories.Remove(category);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
