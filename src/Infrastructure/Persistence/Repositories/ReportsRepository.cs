using Application.Analytics.Dtos;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports.Dtos;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class ReportsRepository : IReportsRepository
{
    private readonly AppDbContext _context;

    public ReportsRepository(AppDbContext context)
    {
        _context = context;
    }

    private sealed record OrderRow(
        int Id,
        string OrderNumber,
        string? ReceiptNumber,
        int TableId,
        string TableName,
        RestaurantTableType TableType,
        bool IsDelivery,
        string WaiterName,
        int? GuestCount,
        DateTime OpenedAt,
        DateTime? PaidAt,
        bool IsPaid,
        decimal Total,
        decimal Discount,
        decimal ServiceCharge,
        PaymentMethod? PaymentMethod);

    private enum SaleKind { Counter, Table, Package }

    private static SaleKind KindOf(OrderRow o) =>
        o.IsDelivery || o.TableType == RestaurantTableType.Delivery ? SaleKind.Package
        : o.TableType == RestaurantTableType.TakeAway ? SaleKind.Counter
        : SaleKind.Table;

    public async Task<BusinessReportResponse> GetBusinessReportAsync(
        int companyId,
        BusinessReportRequest request,
        ReportWindow window,
        CancellationToken cancellationToken)
    {
        var from = window.FromUtc;
        var to = window.ToUtc;
        int? restaurantId = request.RestaurantId is > 0 ? request.RestaurantId : null;
        var paidOrders = await SelectRows(_context.Orders.AsNoTracking()
                .Where(o => o.CompanyId == companyId
                    && o.IsPaid && o.PaidAt != null && o.PaidAt >= from && o.PaidAt < to
                    && (restaurantId == null || o.RestaurantId == restaurantId)))
            .ToListAsync(cancellationToken);

        var openOrders = await SelectRows(_context.Orders.AsNoTracking()
                .Where(o => o.CompanyId == companyId
                    && !o.IsPaid && o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Paid
                    && o.OpenedAt >= from && o.OpenedAt < to
                    && (restaurantId == null || o.RestaurantId == restaurantId)))
            .ToListAsync(cancellationToken);

        var saleOrders = paidOrders.Concat(openOrders).ToList();
        var paidIds = paidOrders.Select(o => o.Id).ToList();
        var saleIds = saleOrders.Select(o => o.Id).ToList();

        var sales = new BusinessReportSalesDto
        {
            Counter = saleOrders.Where(o => KindOf(o) == SaleKind.Counter).Sum(o => o.Total),
            Table = saleOrders.Where(o => KindOf(o) == SaleKind.Table).Sum(o => o.Total),
            Package = saleOrders.Where(o => KindOf(o) == SaleKind.Package).Sum(o => o.Total),
            Discount = saleOrders.Sum(o => o.Discount),
        };
        sales.Total = sales.Counter + sales.Table + sales.Package;

        var payments = await _context.OrderPayments.AsNoTracking()
            .Where(p => p.CompanyId == companyId && paidIds.Contains(p.OrderId))
            .Select(p => new { p.OrderId, p.Method, p.BankCounterpartyId, Amount = p.Amount + (p.ServiceChargeAmount ?? 0) })
            .ToListAsync(cancellationToken);
        var paymentsByOrder = payments.ToLookup(p => p.OrderId);

        // The bank of orders paid by card in one go (parts carry theirs on the payment row).
        var singleBankByOrder = await _context.Orders.AsNoTracking()
            .Where(o => paidIds.Contains(o.Id) && o.BankCounterpartyId != null)
            .Select(o => new { o.Id, o.BankCounterpartyId })
            .ToDictionaryAsync(o => o.Id, o => o.BankCounterpartyId, cancellationToken);

        decimal cash = 0, bank = 0;
        var bankTotals = new Dictionary<int, decimal>();
        decimal bankUnassigned = 0;
        void AddCard(int? bankId, decimal amount)
        {
            bank += amount;
            if (bankId is { } id) bankTotals[id] = bankTotals.GetValueOrDefault(id) + amount;
            else bankUnassigned += amount;
        }
        foreach (var o in paidOrders)
        {
            if (paymentsByOrder.Contains(o.Id))
            {
                foreach (var p in paymentsByOrder[o.Id])
                {
                    if (p.Method == PaymentMethod.Cash) cash += p.Amount;
                    else if (p.Method == PaymentMethod.Card) AddCard(p.BankCounterpartyId, p.Amount);
                }
            }
            else if (o.PaymentMethod == PaymentMethod.Cash) cash += o.Total;
            else if (o.PaymentMethod == PaymentMethod.Card) AddCard(singleBankByOrder.GetValueOrDefault(o.Id), o.Total);
        }

        var bankNames = await _context.Counterparties.AsNoTracking()
            .Where(c => bankTotals.Keys.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var bankBreakdown = bankTotals
            .Select(kv => new BusinessReportBankDto { BankName = bankNames.GetValueOrDefault(kv.Key, "Bank"), Amount = kv.Value })
            .ToList();
        if (bankUnassigned > 0)
            bankBreakdown.Add(new BusinessReportBankDto { BankName = "Bank göstərilməyib", Amount = bankUnassigned });
        bankBreakdown = bankBreakdown.OrderByDescending(b => b.Amount).ToList();

        var cashMovements = await _context.CashMovements.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.CreatedAtUtc >= from && c.CreatedAtUtc < to
                && (restaurantId == null || c.RestaurantId == restaurantId))
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.Amount,
                c.Reason,
                c.CreatedAtUtc,
                CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.FullName : null,
            })
            .ToListAsync(cancellationToken);
        var cashIn = cashMovements.Where(c => c.Type == CashMovementType.Deposit).Sum(c => c.Amount);
        var cashOut = cashMovements.Where(c => c.Type == CashMovementType.Withdrawal).Sum(c => c.Amount);

        var cashReturns = await _context.SaleReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.CreatedAtUtc >= from && r.CreatedAtUtc < to
                && r.PaymentMethod == PaymentMethod.Cash
                && (restaurantId == null || r.RestaurantId == restaurantId))
            .SumAsync(r => (decimal?)r.TotalAmount, cancellationToken) ?? 0;

        var income = new BusinessReportIncomeDto
        {
            Cash = cash,
            Bank = bank,
            BankBreakdown = bankBreakdown,
            Total = cash + bank,
            CashIn = cashIn,
            CashOut = cashOut,
            CashReturns = cashReturns,
            CashRemaining = cash + cashIn - cashOut - cashReturns,
        };

        var cancellations = await _context.OrderCancellations.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.CreatedAtUtc >= from && c.CreatedAtUtc < to
                && (restaurantId == null || c.RestaurantId == restaurantId))
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new SalesReportCancellationDto
            {
                OrderId = c.OrderId,
                CancelledAt = c.CreatedAtUtc,
                TableName = c.TableName,
                OrderNumber = c.ReceiptNumber ?? c.OrderNumber,
                IsWholeOrder = c.IsWholeOrder,
                MenuItemName = c.MenuItemName,
                Quantity = c.Quantity,
                Amount = c.Amount,
                Reason = c.Reason,
                Note = c.Note,
                BeforeKitchen = c.BeforeKitchen,
                CancelledBy = c.CancelledByName,
            })
            .ToListAsync(cancellationToken);

        // ── Gifts (on the same orders the sales cover) ──
        var giftRows = await _context.OrderLines.AsNoTracking()
            .Where(l => l.CompanyId == companyId && saleIds.Contains(l.OrderId) && l.IsGift
                && l.Status != OrderLineStatus.Cancelled)
            .Select(l => new
            {
                l.OrderId,
                MenuItemName = l.MenuItem.Name,
                l.Quantity,
                l.UnitPrice,
                l.GiftedAtUtc,
                l.GiftedByUserId,
            })
            .ToListAsync(cancellationToken);
        var gifterIds = giftRows.Where(g => g.GiftedByUserId != null).Select(g => g.GiftedByUserId!.Value).Distinct().ToList();
        var gifterNames = await _context.Users.AsNoTracking()
            .Where(u => gifterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "", cancellationToken);
        var orderById = saleOrders.ToDictionary(o => o.Id);
        var gifts = giftRows
            .Select(g =>
            {
                var o = orderById[g.OrderId];
                return new SalesReportGiftDto
                {
                    OrderId = g.OrderId,
                    OrderNumber = o.OrderNumber,
                    ReceiptNumber = o.ReceiptNumber,
                    TableName = string.IsNullOrEmpty(o.TableName) ? "—" : o.TableName,
                    MenuItemName = g.MenuItemName,
                    Quantity = g.Quantity,
                    Value = g.UnitPrice * g.Quantity,
                    GiftedAt = g.GiftedAtUtc,
                    GiftedBy = g.GiftedByUserId is int id && gifterNames.TryGetValue(id, out var name) ? name : "—",
                };
            })
            .OrderByDescending(g => g.GiftedAt ?? DateTime.MinValue)
            .ToList();
        sales.Gift = gifts.Sum(g => g.Value);

        // ── Counterparty: sales put on account ──
        var debtEntries = await _context.CounterpartyDebtEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.CreatedAtUtc >= from && e.CreatedAtUtc < to
                && (restaurantId == null || e.OrderId == null || e.Order!.RestaurantId == restaurantId))
            .Select(e => new
            {
                e.CounterpartyId,
                CounterpartyName = e.Counterparty.Name,
                CurrentDebt = e.Counterparty.CurrentDebtAmount,
                e.Type,
                e.Amount,
            })
            .ToListAsync(cancellationToken);
        var creditSales = debtEntries.Where(e => e.Type == CounterpartyDebtEntryType.CreditSale).Sum(e => e.Amount);

        var summary = new BusinessReportSummaryDto
        {
            CheckCount = paidOrders.Count,
            CancelledCount = cancellations.Count,
            CancelledAmount = cancellations.Sum(c => c.Amount),
            OpenCheckCount = openOrders.Count,
            OpenCheckAmount = openOrders.Sum(o => o.Total),
            GiftCount = gifts.Count,
            GiftAmount = sales.Gift,
            Expenses = cashOut,
            CounterpartyCharges = creditSales,
            CurrentIncomeCash = cashIn,
            CurrentIncomeBank = 0,
        };

        var response = new BusinessReportResponse
        {
            Period = request.Period.ToString(),
            FromLocal = window.FromLocal,
            ToLocal = window.ToLocal,
            FromUtc = from,
            ToUtc = to,
            RestaurantId = restaurantId,
            Summary = summary,
            Sales = sales,
            Income = income,
        };

        if (request.ProductSales || request.ProductSalesByCategory)
        {
            var lines = await _context.OrderLines.AsNoTracking()
                .Where(l => l.CompanyId == companyId && saleIds.Contains(l.OrderId) && l.Status != OrderLineStatus.Cancelled)
                .Select(l => new
                {
                    l.MenuItemId,
                    ItemName = l.MenuItem.Name,
                    CategoryId = l.MenuItem.MenuCategoryId,
                    CategoryName = l.MenuItem.MenuCategory.Name,
                    l.Quantity,
                    l.LineTotal,
                })
                .ToListAsync(cancellationToken);

            var products = lines
                .GroupBy(l => new { l.MenuItemId, l.ItemName, l.CategoryId, l.CategoryName })
                .Select(g => new
                {
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    Line = new SalesReportProductLineDto
                    {
                        MenuItemId = g.Key.MenuItemId,
                        Name = g.Key.ItemName,
                        Quantity = g.Sum(l => l.Quantity),
                        Revenue = g.Sum(l => l.LineTotal),
                    },
                })
                .ToList();
            var productTotal = products.Sum(p => p.Line.Revenue);
            foreach (var p in products)
                p.Line.Percent = Percent(p.Line.Revenue, productTotal);

            if (request.ProductSales)
                response.ProductSales = products.Select(p => p.Line).OrderByDescending(p => p.Revenue).ToList();

            if (request.ProductSalesByCategory)
            {
                response.ProductSalesByCategory = products
                    .GroupBy(p => new { p.CategoryId, p.CategoryName })
                    .Select(g => new ReportCategoryGroupDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        Quantity = g.Sum(p => p.Line.Quantity),
                        Revenue = g.Sum(p => p.Line.Revenue),
                        Percent = Percent(g.Sum(p => p.Line.Revenue), productTotal),
                        Products = g.Select(p => p.Line).OrderByDescending(p => p.Revenue).ToList(),
                    })
                    .OrderByDescending(c => c.Revenue)
                    .ToList();
            }
        }

        if (request.CounterpartyDetails)
        {
            response.CounterpartyDetails = debtEntries
                .GroupBy(e => new { e.CounterpartyId, e.CounterpartyName, e.CurrentDebt })
                .Select(g => new ReportCounterpartyLineDto
                {
                    CounterpartyId = g.Key.CounterpartyId,
                    Name = g.Key.CounterpartyName,
                    CreditSales = g.Where(e => e.Type == CounterpartyDebtEntryType.CreditSale).Sum(e => e.Amount),
                    DebtAdded = g.Where(e => e.Type == CounterpartyDebtEntryType.Added).Sum(e => e.Amount),
                    CurrentDebt = g.Key.CurrentDebt,
                })
                .OrderByDescending(c => c.CreditSales + c.DebtAdded)
                .ToList();
        }

        if (request.Expenses)
        {
            response.Expenses = cashMovements
                .Where(c => c.Type == CashMovementType.Withdrawal)
                .Select(c => new ReportExpenseLineDto
                {
                    Id = c.Id,
                    CreatedAt = c.CreatedAtUtc,
                    Amount = c.Amount,
                    Reason = c.Reason,
                    CreatedBy = c.CreatedBy,
                })
                .ToList();
        }

        if (request.OpenChecks)
        {
            response.OpenChecks = openOrders
                .OrderBy(o => o.OpenedAt)
                .Select(o => new ReportOpenCheckDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    TableId = o.TableId,
                    TableName = o.TableName,
                    WaiterName = o.WaiterName,
                    GuestCount = o.GuestCount,
                    OpenedAt = o.OpenedAt,
                    Amount = o.Total,
                })
                .ToList();
        }

        if (request.Gifts)
            response.Gifts = gifts;

        if (request.Cancellations)
            response.Cancellations = cancellations;

        if (request.TableSales)
        {
            response.TableSales = saleOrders
                .Where(o => KindOf(o) == SaleKind.Table)
                .Select(o => new SalesReportReceiptDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    ReceiptNumber = o.ReceiptNumber,
                    TableId = o.TableId,
                    TableName = string.IsNullOrEmpty(o.TableName) ? $"#{o.TableId}" : o.TableName,
                    WaiterName = o.WaiterName,
                    GuestCount = o.GuestCount,
                    OpenedAt = o.OpenedAt,
                    PaidAt = o.PaidAt,
                    PaymentMethod = !o.IsPaid ? null : PaymentLabel(o, paymentsByOrder[o.Id].Select(p => p.Method)),
                    Amount = o.Total,
                    DiscountAmount = o.Discount,
                    ServiceCharge = o.ServiceCharge,
                })
                .GroupBy(r => new { r.TableId, r.TableName })
                .Select(g => new SalesReportTableLineDto
                {
                    TableId = g.Key.TableId,
                    TableName = g.Key.TableName,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(r => r.Amount),
                    Sessions = g.OrderBy(r => r.OpenedAt).ToList(),
                })
                .OrderByDescending(t => t.Revenue)
                .ToList();
        }

        if (request.StockOperations)
        {
            var movements = await _context.StockMovements.AsNoTracking()
                .Where(m => m.CompanyId == companyId && m.MovementDate >= from && m.MovementDate < to
                    && (restaurantId == null || m.Warehouse.RestaurantId == restaurantId))
                .Select(m => new
                {
                    m.StockItemId,
                    ItemName = m.StockItem.Name,
                    m.StockItem.Unit,
                    m.Type,
                    m.Quantity,
                })
                .ToListAsync(cancellationToken);

            response.StockOperations = movements
                .GroupBy(m => new { m.StockItemId, m.ItemName, m.Unit })
                .Select(g => new ReportStockOperationLineDto
                {
                    StockItemId = g.Key.StockItemId,
                    StockItemName = g.Key.ItemName,
                    Unit = g.Key.Unit.ToString(),
                    Received = g.Where(m => m.Type == StockMovementType.StockEntryIn).Sum(m => m.Quantity),
                    TransferIn = g.Where(m => m.Type == StockMovementType.TransferIn).Sum(m => m.Quantity),
                    TransferOut = g.Where(m => m.Type == StockMovementType.TransferOut).Sum(m => m.Quantity),
                    Consumed = g.Where(m => m.Type == StockMovementType.OrderConsumptionOut).Sum(m => m.Quantity)
                        - g.Where(m => m.Type == StockMovementType.OrderConsumptionReversalIn).Sum(m => m.Quantity),
                    Returned = g.Where(m => m.Type == StockMovementType.SaleReturnIn).Sum(m => m.Quantity),
                })
                .OrderBy(l => l.StockItemName)
                .ToList();
        }

        if (request.StockBalance)
        {
            var balances = await _context.WarehouseStocks.AsNoTracking()
                .Where(s => s.CompanyId == companyId && s.Quantity != 0
                    && (restaurantId == null || s.Warehouse.RestaurantId == restaurantId))
                .Select(s => new
                {
                    s.WarehouseId,
                    WarehouseName = s.Warehouse.Name,
                    s.StockItemId,
                    ItemName = s.StockItem.Name,
                    s.StockItem.Unit,
                    s.Quantity,
                })
                .ToListAsync(cancellationToken);

            response.StockBalance = balances
                .Select(s => new ReportStockBalanceLineDto
                {
                    WarehouseId = s.WarehouseId,
                    WarehouseName = s.WarehouseName,
                    StockItemId = s.StockItemId,
                    StockItemName = s.ItemName,
                    Unit = s.Unit.ToString(),
                    Quantity = s.Quantity,
                })
                .OrderBy(s => s.WarehouseName)
                .ThenBy(s => s.StockItemName)
                .ToList();
        }

        return response;
    }

    private static IQueryable<OrderRow> SelectRows(IQueryable<Domain.Entities.Order> orders) =>
        orders.Select(o => new OrderRow(
            o.Id,
            o.OrderNumber,
            o.ReceiptNumber,
            o.TableId,
            o.Table != null ? o.Table.Name : "",
            o.Table != null ? o.Table.Type : RestaurantTableType.Masa,
            o.IsDelivery,
            o.Waiter != null ? (o.Waiter.FirstName + " " + o.Waiter.LastName).Trim() : "Naməlum",
            o.GuestCount,
            o.OpenedAt,
            o.PaidAt,
            o.IsPaid,
            o.TotalAmount,
            o.DiscountAmount,
            o.ServiceChargeAmount ?? 0,
            o.PaymentMethod));

    private static decimal Percent(decimal part, decimal total) =>
        total > 0 ? Math.Round(part / total * 100, 1) : 0;

    /// <summary>"Cash", "Card" or "Cash+Card" when a table paid in parts by different methods.</summary>
    private static string? PaymentLabel(OrderRow order, IEnumerable<PaymentMethod> partMethods)
    {
        var methods = partMethods.Distinct().OrderBy(m => m).ToList();
        return methods.Count > 0
            ? string.Join("+", methods)
            : order.PaymentMethod?.ToString();
    }
}
