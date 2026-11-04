using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreCustomerRepository : IStoreCustomerRepository
{
    private readonly AppDbContext _context;

    public StoreCustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<StoreCustomer>> SearchCustomersAsync(int companyId, string? term, bool includeInactive, int take, CancellationToken ct)
    {
        var query = _context.StoreCustomers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (includeInactive || x.IsActive));
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(x => x.Name.Contains(t) || x.Code.Contains(t) || (x.Phone != null && x.Phone.Contains(t)));
        }

        return query.OrderBy(x => x.Name).Take(take).ToListAsync(ct);
    }

    public Task<StoreCustomer?> GetCustomerAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreCustomers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task<StoreCustomer?> FindCustomerAsync(int companyId, string term, CancellationToken ct)
    {
        var t = term.Trim();
        var exact = await _context.StoreCustomers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive && (x.Code == t || x.Phone == t), ct);
        if (exact is not null)
            return exact;

        var partial = await _context.StoreCustomers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && (x.Name.Contains(t) || (x.Phone != null && x.Phone.Contains(t))))
            .Take(2)
            .ToListAsync(ct);
        return partial.Count == 1 ? partial[0] : null;
    }

    public Task<bool> CodeExistsAsync(int companyId, string code, int? exceptId, CancellationToken ct)
        => _context.StoreCustomers.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.Id != exceptId, ct);

    public async Task AddCustomerAsync(StoreCustomer customer, CancellationToken ct)
        => await _context.StoreCustomers.AddAsync(customer, ct);

    public async Task AddBonusEntryAsync(StoreBonusEntry entry, CancellationToken ct)
        => await _context.StoreBonusEntries.AddAsync(entry, ct);

    public async Task<decimal> GetBonusBalanceAsync(int companyId, int customerId, CancellationToken ct)
        => await _context.StoreBonusEntries
            .Where(x => x.CompanyId == companyId && x.StoreCustomerId == customerId)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

    public Task<Dictionary<int, decimal>> GetBonusBalancesAsync(int companyId, CancellationToken ct)
        => _context.StoreBonusEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .GroupBy(x => x.StoreCustomerId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);

    public Task<List<StoreBonusEntry>> GetBonusEntriesAsync(
        int companyId, int customerId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct)
        => _context.StoreBonusEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StoreCustomerId == customerId
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive))
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task AddCustomerPaymentAsync(StoreCustomerPayment payment, CancellationToken ct)
        => await _context.StoreCustomerPayments.AddAsync(payment, ct);

    public Task<StoreCustomerPayment?> GetCustomerPaymentAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreCustomerPayments.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreCustomerPayment>> GetCustomerPaymentsAsync(int companyId, int customerId, CancellationToken ct)
        => _context.StoreCustomerPayments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StoreCustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);

    public Task<List<StoreCustomerPayment>> GetPaymentsForShiftAsync(int shiftId, CancellationToken ct)
        => _context.StoreCustomerPayments.AsNoTracking()
            .Where(x => x.StoreCashShiftId == shiftId && x.ReversedAtUtc == null)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
