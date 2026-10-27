using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Finance.Dtos;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FinanceRepository : IFinanceRepository
{
    private readonly AppDbContext _context;

    public FinanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FinanceEntry entry, CancellationToken cancellationToken)
    {
        await _context.FinanceEntries.AddAsync(entry, cancellationToken);
    }

    public Task<FinanceEntry?> GetByIdAsync(int companyId, int id, CancellationToken cancellationToken) =>
        _context.FinanceEntries
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);

    public void Remove(FinanceEntry entry) => _context.FinanceEntries.Remove(entry);

    public Task<List<FinanceEntry>> GetEntriesAsync(
        int companyId, int? restaurantId, DateTime fromDate, DateTime toDateExclusive,
        FinanceEntryType? type, CancellationToken cancellationToken) =>
        _context.FinanceEntries
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Where(x => x.CompanyId == companyId
                && x.EntryDate >= fromDate && x.EntryDate < toDateExclusive
                && (restaurantId == null || x.RestaurantId == restaurantId)
                && (type == null || x.Type == type))
            .OrderByDescending(x => x.EntryDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<FinanceSourceLine>> GetAutomaticLinesAsync(
        int companyId, int? restaurantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var lines = new List<FinanceSourceLine>();

        // Sales: every paid check, on the day it was paid.
        var sales = await _context.Orders.AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.IsPaid && o.PaidAt != null
                && o.PaidAt >= fromUtc && o.PaidAt < toUtc
                && (restaurantId == null || o.RestaurantId == restaurantId))
            .Select(o => new { PaidAt = o.PaidAt!.Value, o.TotalAmount })
            .ToListAsync(cancellationToken);
        lines.AddRange(sales.Select(s =>
            new FinanceSourceLine(s.PaidAt, FinanceEntryType.Income, "Satış gəliri", s.TotalAmount, "Sales")));

        // Goods bought: completed stock purchases, valued in AZN.
        var purchases = await _context.StockPurchases.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Status == StockPurchaseStatus.Completed
                && p.PurchaseDate >= fromUtc && p.PurchaseDate < toUtc
                && (restaurantId == null || p.Warehouse.RestaurantId == restaurantId))
            .Select(p => new { p.PurchaseDate, Amount = p.Lines.Sum(l => l.Quantity * l.UnitPriceAzn) })
            .ToListAsync(cancellationToken);
        lines.AddRange(purchases.Select(p =>
            new FinanceSourceLine(p.PurchaseDate, FinanceEntryType.Expense, "Mal alışı", p.Amount, "StockPurchase")));

        // Money taken out of the till, named by the reason typed at the time.
        var withdrawals = await _context.CashMovements.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.Type == CashMovementType.Withdrawal
                && c.CreatedAtUtc >= fromUtc && c.CreatedAtUtc < toUtc
                && (restaurantId == null || c.RestaurantId == restaurantId))
            .Select(c => new { c.CreatedAtUtc, c.Amount, c.Reason })
            .ToListAsync(cancellationToken);
        lines.AddRange(withdrawals.Select(c => new FinanceSourceLine(
            c.CreatedAtUtc, FinanceEntryType.Expense,
            string.IsNullOrWhiteSpace(c.Reason) ? "Kassa xərci" : c.Reason.Trim(),
            c.Amount, "CashRegister")));

        // Sales taken back.
        var returns = await _context.SaleReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.CreatedAtUtc >= fromUtc && r.CreatedAtUtc < toUtc
                && (restaurantId == null || r.RestaurantId == restaurantId))
            .Select(r => new { r.CreatedAtUtc, r.TotalAmount })
            .ToListAsync(cancellationToken);
        lines.AddRange(returns.Select(r =>
            new FinanceSourceLine(r.CreatedAtUtc, FinanceEntryType.Expense, "Satış qaytarmaları", r.TotalAmount, "SaleReturn")));

        return lines;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
