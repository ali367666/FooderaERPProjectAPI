using Application.Analytics;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Queries.SalesDocuments;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SalesDocumentRepository : ISalesDocumentRepository
{
    private readonly AppDbContext _context;

    public SalesDocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Domain.Entities.Order> Documents(SalesDocumentFilter filter)
    {
        var orders = _context.Orders.AsNoTracking()
            .Where(o => o.CompanyId == filter.CompanyId && (o.IsPaid || o.Status == OrderStatus.Cancelled));

        if (filter.FromUtc is { } from)
            orders = orders.Where(o => (o.PaidAt ?? o.ClosedAt ?? o.OpenedAt) >= from);
        if (filter.ToUtcExclusive is { } to)
            orders = orders.Where(o => (o.PaidAt ?? o.ClosedAt ?? o.OpenedAt) < to);
        if (filter.RestaurantId is { } restaurantId)
            orders = orders.Where(o => o.RestaurantId == restaurantId);
        if (filter.Search is { } search)
            orders = orders.Where(o => o.OrderNumber.Contains(search)
                || (o.ReceiptNumber != null && o.ReceiptNumber.Contains(search))
                || o.Table.Name.Contains(search));

        return orders;
    }

    public async Task<SalesDocumentsSummary> GetSummaryAsync(SalesDocumentFilter filter, CancellationToken cancellationToken)
    {
        var rows = await Documents(filter)
            .GroupBy(o => o.Status == OrderStatus.Cancelled)
            .Select(g => new { Cancelled = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount), Discount = g.Sum(o => o.DiscountAmount) })
            .ToListAsync(cancellationToken);
        var paid = rows.FirstOrDefault(r => !r.Cancelled);

        return new SalesDocumentsSummary
        {
            TotalSales = paid?.Total ?? 0,
            ReceiptCount = paid?.Count ?? 0,
            TotalDiscount = paid?.Discount ?? 0,
            CancelledCount = rows.FirstOrDefault(r => r.Cancelled)?.Count ?? 0
        };
    }

    public async Task<(List<SalesDocumentRow> Items, int Total)> GetPageAsync(
        SalesDocumentFilter filter, int skip, int take, CancellationToken cancellationToken)
    {
        var documents = Documents(filter);
        if (filter.Status == SalesDocumentStatus.Paid)
            documents = documents.Where(o => o.Status != OrderStatus.Cancelled);
        else if (filter.Status == SalesDocumentStatus.Cancelled)
            documents = documents.Where(o => o.Status == OrderStatus.Cancelled);

        var total = await documents.CountAsync(cancellationToken);
        var rows = await documents
            .OrderByDescending(o => o.PaidAt ?? o.ClosedAt ?? o.OpenedAt)
            .ThenByDescending(o => o.Id)
            .Skip(skip)
            .Take(take)
            .Select(o => new
            {
                o.Id,
                o.CompanyId,
                o.RestaurantId,
                RestaurantName = o.Restaurant.Name,
                o.ReceiptNumber,
                o.OrderNumber,
                Date = o.PaidAt ?? o.ClosedAt ?? o.OpenedAt,
                o.TableId,
                TableName = o.Table.Name,
                WaiterFirstName = o.Waiter.FirstName,
                WaiterLastName = o.Waiter.LastName,
                o.TotalAmount,
                o.DiscountAmount,
                o.PaymentMethod,
                o.IsPaid,
                o.Status,
                ProcessedBy = o.ProcessedByUser != null ? o.ProcessedByUser.FullName : null
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.Id).ToList();
        var money = await OrderPaymentSplit.ComputeAsync(
            _context,
            rows.Where(r => r.IsPaid && r.Status != OrderStatus.Cancelled)
                .Select(r => (r.Id, r.PaymentMethod, r.TotalAmount)).ToList(),
            cancellationToken);
        var returned = (await _context.SaleReturns.AsNoTracking()
                .Where(r => r.CompanyId == filter.CompanyId && ids.Contains(r.OrderId))
                .Select(r => new { r.OrderId, r.TotalAmount })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.OrderId, r => r.TotalAmount);

        var items = rows.Select(r =>
        {
            var split = money.GetValueOrDefault(r.Id, MoneySplit.None);
            return new SalesDocumentRow
            {
                Id = r.Id,
                CompanyId = r.CompanyId,
                RestaurantId = r.RestaurantId,
                RestaurantName = r.RestaurantName,
                Number = r.ReceiptNumber ?? r.OrderNumber,
                OrderNumber = r.OrderNumber,
                DateUtc = r.Date,
                TableId = r.TableId,
                TableName = r.TableName,
                WaiterName = $"{r.WaiterFirstName} {r.WaiterLastName}".Trim(),
                TotalAmount = r.TotalAmount,
                DiscountAmount = r.DiscountAmount,
                DiscountPercent = SalesReportMath.Percent(r.DiscountAmount, r.TotalAmount + r.DiscountAmount),
                CashAmount = split.Cash,
                CardAmount = split.Card,
                CreditAmount = split.Credit,
                ReturnedAmount = returned[r.Id].Sum(),
                Status = r.Status == OrderStatus.Cancelled ? SalesDocumentStatus.Cancelled : SalesDocumentStatus.Paid,
                ProcessedByUserName = r.ProcessedBy
            };
        }).ToList();

        return (items, total);
    }
}
