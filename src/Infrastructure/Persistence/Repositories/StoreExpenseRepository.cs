using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreExpenseRepository : IStoreExpenseRepository
{
    private readonly AppDbContext _context;

    public StoreExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<StoreExpenseItem>> GetItemsAsync(int companyId, bool includeInactive, CancellationToken ct)
        => _context.StoreExpenseItems.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (includeInactive || x.IsActive))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<StoreExpenseItem?> GetItemAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreExpenseItems.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<bool> ItemNameExistsAsync(int companyId, string name, int? exceptId, CancellationToken ct)
        => _context.StoreExpenseItems.AnyAsync(x => x.CompanyId == companyId && x.Name == name && (exceptId == null || x.Id != exceptId), ct);

    public async Task AddItemAsync(StoreExpenseItem item, CancellationToken ct)
        => await _context.StoreExpenseItems.AddAsync(item, ct);

    public Task<StoreExpense?> GetExpenseAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreExpenses
            .Include(x => x.Lines).ThenInclude(l => l.StoreCashRegister)
            .Include(x => x.Lines).ThenInclude(l => l.StoreExpenseItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreExpense>> SearchAsync(int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct)
        => _context.StoreExpenses.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.StoreCashRegister)
            .Include(x => x.Lines).ThenInclude(l => l.StoreExpenseItem)
            .Where(x => x.CompanyId == companyId
                && (fromUtc == null || x.DocumentDate >= fromUtc)
                && (toUtcExclusive == null || x.DocumentDate < toUtcExclusive))
            .OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.Id)
            .Take(1000)
            .ToListAsync(ct);

    public async Task AddExpenseAsync(StoreExpense expense, CancellationToken ct)
        => await _context.StoreExpenses.AddAsync(expense, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
