using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SaleReturnRepository : ISaleReturnRepository
{
    private readonly AppDbContext _context;

    public SaleReturnRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SaleReturn saleReturn, CancellationToken cancellationToken)
    {
        await _context.SaleReturns.AddAsync(saleReturn, cancellationToken);
    }

    /// <summary>
    /// The receipt barcode carries the order id (zero-padded digits); the full receipt number
    /// typed by hand is accepted too.
    /// </summary>
    public async Task<Order?> FindPaidOrderForReturnAsync(int companyId, string code, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();

        var query = _context.Orders
            .Include(x => x.Restaurant)
            .Include(x => x.Table)
            .Include(x => x.Waiter)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.CompanyId == companyId && x.Status == OrderStatus.Paid);

        if (int.TryParse(trimmed, out var orderId))
            return await query.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        return await query.FirstOrDefaultAsync(x => x.ReceiptNumber == trimmed, cancellationToken);
    }

    public async Task<List<SaleReturn>> GetByOrderIdAsync(int companyId, int orderId, CancellationToken cancellationToken)
    {
        return await _context.SaleReturns
            .Include(x => x.CreatedByUser)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.CompanyId == companyId && x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SaleReturn>> GetBetweenAsync(
        int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return await _context.SaleReturns
            .Include(x => x.Order)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId
                && x.CreatedAtUtc >= from && x.CreatedAtUtc < to)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
