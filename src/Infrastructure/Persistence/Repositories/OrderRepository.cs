using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Restaurant)
            .Include(x => x.Table)
                .ThenInclude(x => x!.Section)
            .Include(x => x.BanquetTables).ThenInclude(b => b.Table)
            .Include(x => x.Waiter)
            .Include(x => x.ProcessedByUser)
            .Include(x => x.Counterparty)
            .Include(x => x.DeliveryDriverEmployee)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<int>> ClaimKitchenLinesAsync(IReadOnlyCollection<int> lineIds, DateTime at, CancellationToken cancellationToken)
    {
        if (lineIds.Count == 0) return new();

        // Only lines still unsent are updated, so a second OK pressed at the same moment claims nothing.
        await _context.OrderLines
            .Where(l => lineIds.Contains(l.Id) && l.KitchenPrintedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.KitchenPrintedAt, at), cancellationToken);

        return await _context.OrderLines.AsNoTracking()
            .Where(l => lineIds.Contains(l.Id) && l.KitchenPrintedAt == at)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ReleaseKitchenLinesAsync(IReadOnlyCollection<int> lineIds, CancellationToken cancellationToken)
    {
        if (lineIds.Count == 0) return;
        await _context.OrderLines
            .Where(l => lineIds.Contains(l.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.KitchenPrintedAt, (DateTime?)null), cancellationToken);
    }

    public async Task<List<Application.Orders.Queries.Receipts.ReceiptListItem>> GetReceiptsAsync(
        int companyId, int? restaurantId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken)
    {
        var rows = await _context.Orders.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.IsPaid
                && x.PaidAt != null && x.PaidAt >= fromUtc && x.PaidAt < toUtcExclusive
                && (restaurantId == null || x.RestaurantId == restaurantId))
            .OrderByDescending(x => x.PaidAt)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.ReceiptNumber,
                x.RestaurantId,
                RestaurantName = x.Restaurant != null ? x.Restaurant.Name : null,
                TableName = x.Table != null ? x.Table.Name : null,
                WaiterFirst = x.Waiter != null ? x.Waiter.FirstName : null,
                WaiterLast = x.Waiter != null ? x.Waiter.LastName : null,
                PaidAt = x.PaidAt!.Value,
                x.PaymentMethod,
                x.DiscountAmount,
                x.TotalAmount,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new Application.Orders.Queries.Receipts.ReceiptListItem
        {
            Id = r.Id,
            OrderNumber = r.OrderNumber,
            ReceiptNumber = r.ReceiptNumber,
            RestaurantId = r.RestaurantId,
            RestaurantName = r.RestaurantName,
            TableName = r.TableName,
            WaiterName = r.WaiterFirst is null ? null : $"{r.WaiterFirst} {r.WaiterLast}".Trim(),
            PaidAt = r.PaidAt,
            PaymentMethod = r.PaymentMethod?.ToString(),
            DiscountAmount = r.DiscountAmount,
            TotalAmount = r.TotalAmount,
        }).ToList();
    }

    public async Task<List<Order>> GetAllAsync(int companyId, Application.Orders.Queries.GetAll.OrderListFilter? filter, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(x => x.Restaurant)
            .Include(x => x.Table)
            .Include(x => x.BanquetTables).ThenInclude(b => b.Table)
            .Include(x => x.Waiter)
            .Include(x => x.ProcessedByUser)
            .Include(x => x.Counterparty)
            .Include(x => x.DeliveryDriverEmployee)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.CompanyId == companyId);

        if (filter?.Statuses is { Count: > 0 })
        {
            var statusList = filter.Statuses.ToList();
            query = query.Where(x => statusList.Contains(x.Status));
        }

        if (filter?.ExcludeFinished == true)
            query = query.Where(x => x.Status != Domain.Enums.OrderStatus.Paid && x.Status != Domain.Enums.OrderStatus.Cancelled);

        if (filter?.TableType is { } tableType)
            query = query.Where(x => x.Table != null && x.Table.Type == tableType);

        if (filter?.RestaurantId is { } restaurantId)
            query = query.Where(x => x.RestaurantId == restaurantId);

        // A period keeps the receipts paid inside it.
        if (filter?.PaidFromUtc is { } paidFrom && filter.PaidToUtc is { } paidTo)
            query = query.Where(x => x.PaidAt != null && x.PaidAt >= paidFrom && x.PaidAt < paidTo);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetPaidBetweenAsync(int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId
                && x.RestaurantId == restaurantId
                && x.IsPaid
                && x.PaidAt != null
                && x.PaidAt >= from
                && x.PaidAt <= to)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetActiveForTablesAsync(int companyId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken)
    {
        var ids = tableIds.ToList();
        return await _context.Orders
            .AsNoTracking()
            .Include(x => x.Waiter)
            .Include(x => x.Counterparty)
            .Where(x => x.CompanyId == companyId
                && ids.Contains(x.TableId)
                && x.Status != Domain.Enums.OrderStatus.Paid
                && x.Status != Domain.Enums.OrderStatus.Cancelled)
            .OrderByDescending(x => x.OpenedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenOrderForTableAsync(int tableId, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders.AnyAsync(x =>
            (x.TableId == tableId || x.BanquetTables.Any(b => b.TableId == tableId)) &&
            x.CompanyId == companyId &&
            x.Status != Domain.Enums.OrderStatus.Paid &&
            x.Status != Domain.Enums.OrderStatus.Cancelled,
            cancellationToken);
    }

    public async Task<bool> ExistsByOrderNumberAsync(string orderNumber, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders.AnyAsync(x =>
            x.OrderNumber == orderNumber &&
            x.CompanyId == companyId,
            cancellationToken);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    public void Delete(Order order)
    {
        _context.Orders.Remove(order);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdWithLinesAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<Order?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Include(x => x.Restaurant)
            .Include(x => x.Table)
            .Include(x => x.Waiter)
            .Include(x => x.DeliveryDriverEmployee)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}