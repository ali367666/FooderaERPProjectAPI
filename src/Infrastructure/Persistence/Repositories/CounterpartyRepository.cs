using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CounterpartyRepository : ICounterpartyRepository
{
    private readonly AppDbContext _context;

    public CounterpartyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Counterparty counterparty, CancellationToken cancellationToken)
    {
        await _context.Counterparties.AddAsync(counterparty, cancellationToken);
    }

    public async Task<Counterparty?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Counterparties
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Counterparty>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Counterparties
            .Include(x => x.Category)
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.Counterparties
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public async Task<bool> ExistsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await _context.Counterparties.AnyAsync(x => x.CategoryId == categoryId, cancellationToken);
    }

    public void Update(Counterparty counterparty) => _context.Counterparties.Update(counterparty);
    public void Delete(Counterparty counterparty) => _context.Counterparties.Remove(counterparty);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
