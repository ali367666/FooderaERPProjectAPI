using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Reports;

// ---- DTOs ----------------------------------------------------------------------------------

public class StoreReportRow
{
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Count { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class StoreSalesReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int SalesCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal Vat { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal AverageReceipt { get; set; }

    public int CancelledCount { get; set; }
    public decimal CancelledTotal { get; set; }
    public int ReturnsCount { get; set; }
    public decimal ReturnsTotal { get; set; }
    public decimal ReturnedCost { get; set; }

    /// <summary>Net sales less returns.</summary>
    public decimal NetAfterReturns { get; set; }

    /// <summary>Net after returns less the cost of what was sold (returned sellable goods give their cost back).</summary>
    public decimal Profit { get; set; }

    public List<StoreReportRow> ByDay { get; set; } = new();
    public List<StoreReportRow> ByProduct { get; set; } = new();
    public List<StoreReportRow> ByCategory { get; set; } = new();
    public List<StoreReportRow> ByCashier { get; set; } = new();
    public List<StoreReportRow> ByRegister { get; set; } = new();
    public List<StoreReportRow> ByPayment { get; set; } = new();

    /// <summary>Active products with no sale in the period.</summary>
    public List<StoreUnsoldProduct> Unsold { get; set; } = new();
}

public class StoreUnsoldProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? CategoryName { get; set; }
    public decimal Stock { get; set; }
}

public class StoreStockReportResponse
{
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
    public List<StoreStockBalanceResponse> Rows { get; set; } = new();
    public List<StoreStockBalanceResponse> BelowMinimum { get; set; } = new();
    public List<StoreReportRow> ByWarehouse { get; set; } = new();

    /// <summary>Movements in the period by kind: purchases, sales, transfers, waste, adjustments...</summary>
    public List<StoreReportRow> MovementsByType { get; set; } = new();
}

public class StoreCashReportResponse
{
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<StoreCashReportMovement> Movements { get; set; } = new();
    public List<StoreCashReportShift> Shifts { get; set; } = new();
    public decimal TotalDifference { get; set; }
}

public class StoreCashReportMovement
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string RegisterName { get; set; } = default!;
    public StoreCashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public string? UserName { get; set; }
}

public class StoreCashReportShift
{
    public int ShiftId { get; set; }
    public string RegisterName { get; set; } = default!;
    public string? CashierName { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? CashDifference { get; set; }
}

public class StoreCustomerDebtRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public decimal Debt { get; set; }
    public decimal Age0To30 { get; set; }
    public decimal Age31To60 { get; set; }
    public decimal Age61To90 { get; set; }
    public decimal Age90Plus { get; set; }
    public DateTime? OldestOpenDate { get; set; }
}

public class StoreCustomerDebtReportResponse
{
    public decimal TotalDebt { get; set; }
    public decimal TotalOver30 { get; set; }
    public List<StoreCustomerDebtRow> Customers { get; set; } = new();
}

public class StoreBonusReportRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public decimal Earned { get; set; }
    public decimal Spent { get; set; }
    public decimal Reversed { get; set; }
    public decimal Refunded { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Expired { get; set; }
    public decimal Net { get; set; }

    /// <summary>Balance at the start of the period.</summary>
    public decimal Opening { get; set; }

    /// <summary>Everything that added bonus in the period (earned, refunded, positive adjustments).</summary>
    public decimal Increase { get; set; }

    /// <summary>Everything that took bonus away in the period (paid with, reversed, expired, negative adjustments).</summary>
    public decimal Decrease { get; set; }

    /// <summary>Balance at the end of the period.</summary>
    public decimal Closing { get; set; }

    /// <summary>Balance right now.</summary>
    public decimal Current { get; set; }
}

public class StoreBonusReportResponse
{
    public decimal TotalEarned { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalReversed { get; set; }
    public List<StoreBonusReportRow> Customers { get; set; } = new();
}

/// <summary>
/// The grouped sales report: pick the groupings (in order), the columns, and lists of customers / tills / suppliers / products to keep.
/// Groupings: day, customer, register, supplier, product, document. Columns: quantity, turnover, grossTurnover, cost, profit, profitPercent.
/// </summary>
public class StoreSalesMatrixRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<string> GroupBy { get; set; } = new();
    public List<int>? CustomerIds { get; set; }
    public List<int>? RegisterIds { get; set; }
    public List<int>? SupplierIds { get; set; }
    public List<int>? ProductIds { get; set; }

    /// <summary>A column name to sort every level by (descending when <see cref="SortDesc"/>); empty keeps the grouping order.</summary>
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}

public class StoreSalesMatrixNode
{
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Group { get; set; }
    public decimal Quantity { get; set; }
    public decimal Turnover { get; set; }
    public decimal GrossTurnover { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitPercent { get; set; }
    public List<StoreSalesMatrixNode> Children { get; set; } = new();
}

public class StoreSalesMatrixResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<string> GroupBy { get; set; } = new();
    public StoreSalesMatrixNode Total { get; set; } = new();
}

public class StoreStockSummaryRowResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal OpeningQty { get; set; }
    public decimal OpeningValue { get; set; }
    public decimal InQty { get; set; }
    public decimal InValue { get; set; }
    public decimal OutQty { get; set; }
    public decimal OutValue { get; set; }
    public decimal ClosingQty { get; set; }
    public decimal ClosingValue { get; set; }

    /// <summary>Closing quantity at the sale price.</summary>
    public decimal SaleAmount { get; set; }
}

public class StoreStockSummaryWarehouse
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public StoreStockSummaryRowResponse Total { get; set; } = new();
    public List<StoreStockSummaryRowResponse> Rows { get; set; } = new();
}

public class StoreStockSummaryResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public StoreStockSummaryRowResponse Total { get; set; } = new();
    public List<StoreStockSummaryWarehouse> Warehouses { get; set; } = new();
}

public class StoreProductRatingRow
{
    public int Rank { get; set; }
    public int ProductId { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal SalePrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class StoreProductRatingResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public bool Least { get; set; }

    /// <summary>How many different products sold in the period (the list may show only the first part).</summary>
    public int ProductCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public List<StoreProductRatingRow> Rows { get; set; } = new();
}

public class StoreCashSummaryRow
{
    public int RegisterId { get; set; }
    public string RegisterName { get; set; } = default!;
    public decimal Opening { get; set; }
    public decimal Income { get; set; }
    public decimal Outcome { get; set; }
    public decimal Closing { get; set; }
}

public class StoreCashSummaryResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public StoreCashSummaryRow Total { get; set; } = new();
    public List<StoreCashSummaryRow> Registers { get; set; } = new();
}

// ---- Queries -------------------------------------------------------------------------------

public record GetStoreCashSummaryQuery(DateTime From, DateTime To) : IRequest<StoreCashSummaryResponse>;

public record GetStoreProductRatingQuery(DateTime From, DateTime To, bool Least, int Take) : IRequest<StoreProductRatingResponse>;

public record GetStoreStockSummaryQuery(DateTime From, DateTime To, int? WarehouseId, int? CategoryId) : IRequest<StoreStockSummaryResponse>;

public record GetStoreSalesMatrixQuery(StoreSalesMatrixRequest Request) : IRequest<StoreSalesMatrixResponse>;

public record GetStoreSalesReportQuery(
    DateTime From, DateTime To, int? RestaurantId, int? WarehouseId, int? CashierUserId, int? CategoryId, int? ProductId, bool IncludeUnsold)
    : IRequest<StoreSalesReportResponse>;

public record GetStoreStockReportQuery(DateTime From, DateTime To, int? WarehouseId, int? CategoryId) : IRequest<StoreStockReportResponse>;

public record GetStoreCashReportQuery(DateTime From, DateTime To, int? RestaurantId) : IRequest<StoreCashReportResponse>;

public record GetStoreCustomerDebtReportQuery : IRequest<StoreCustomerDebtReportResponse>;

public record GetStoreBonusReportQuery(DateTime From, DateTime To) : IRequest<StoreBonusReportResponse>;

// ---- Handlers ------------------------------------------------------------------------------

public class StoreReportsHandlers :
    IRequestHandler<GetStoreSalesReportQuery, StoreSalesReportResponse>,
    IRequestHandler<GetStoreStockReportQuery, StoreStockReportResponse>,
    IRequestHandler<GetStoreCashReportQuery, StoreCashReportResponse>,
    IRequestHandler<GetStoreCustomerDebtReportQuery, StoreCustomerDebtReportResponse>,
    IRequestHandler<GetStoreBonusReportQuery, StoreBonusReportResponse>,
    IRequestHandler<GetStoreSalesMatrixQuery, StoreSalesMatrixResponse>,
    IRequestHandler<GetStoreStockSummaryQuery, StoreStockSummaryResponse>,
    IRequestHandler<GetStoreProductRatingQuery, StoreProductRatingResponse>,
    IRequestHandler<GetStoreCashSummaryQuery, StoreCashSummaryResponse>
{
    private static readonly string[] MatrixGroups = { "day", "customer", "register", "supplier", "product", "document" };
    private static readonly string[] MatrixSorts = { "quantity", "turnover", "grossTurnover", "cost", "profit", "profitPercent" };

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private readonly IStoreReportRepository _reports;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreReportsHandlers(IStoreReportRepository reports, IStoreRepository store, ICurrentUserService currentUser, Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _reports = reports;
        _store = store;
        _currentUser = currentUser;
    }

    private static (DateTime FromUtc, DateTime ToUtc) Bounds(DateTime from, DateTime to)
        => (BusinessTime.ToUtc(from.Date + ReportPeriodResolver.DayStart), BusinessTime.ToUtc(to.Date.AddDays(1) + ReportPeriodResolver.DayStart));

    // ---- Sales and profit -------------------------------------------------------------------

    public async Task<StoreSalesReportResponse> Handle(GetStoreSalesReportQuery q, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var (fromUtc, toUtc) = Bounds(q.From, q.To);

        var branches = await _access.AllowedBranchesAsync(ct);
        var lines = await _reports.GetSaleLinesAsync(companyId, fromUtc, toUtc, q.RestaurantId, q.WarehouseId, q.CashierUserId, q.CategoryId, q.ProductId, ct, branches);
        var headers = await _reports.GetSaleHeadersAsync(companyId, fromUtc, toUtc, q.RestaurantId, q.WarehouseId, q.CashierUserId, ct, branches);
        var returns = await _reports.GetReturnsAsync(companyId, fromUtc, toUtc, q.RestaurantId, q.WarehouseId, ct, branches);
        var names = await _store.GetUserNamesAsync(
            lines.Select(l => l.CashierUserId).Concat(headers.Select(h => h.CashierUserId)).OfType<int>(), ct);

        var completed = headers.Where(h => h.Status == StoreSaleStatus.Completed).ToList();
        var cancelled = headers.Where(h => h.Status == StoreSaleStatus.Cancelled).ToList();

        // With a product or category filter only the matching lines count, otherwise the whole receipts.
        var filtered = q.CategoryId != null || q.ProductId != null;
        var net = filtered ? lines.Sum(l => l.LineTotal) : completed.Sum(h => h.Total);
        var cost = lines.Sum(l => l.Cost);
        var returnsTotal = filtered ? 0m : returns.Sum(r => r.Total);
        var returnedCost = filtered ? 0m : returns.Sum(r => r.SellableCost);

        string Day(DateTime utc) => (BusinessTime.FromUtc(utc) - ReportPeriodResolver.DayStart).Date.ToString("yyyy-MM-dd");

        List<StoreReportRow> Group<T>(IEnumerable<T> source, Func<T, string> key, Func<T, string> name, Func<T, decimal> qty, Func<T, decimal> amount, Func<T, decimal> cst)
            => source.GroupBy(key).Select(g => new StoreReportRow
            {
                Key = g.Key,
                Name = name(g.First()),
                Count = g.Count(),
                Quantity = g.Sum(qty),
                Amount = R2(g.Sum(amount)),
                Cost = R2(g.Sum(cst)),
                Profit = R2(g.Sum(amount) - g.Sum(cst)),
            }).ToList();

        var byPayment = completed.SelectMany(h => h.Payments.Select(p => (p.Method, p.Amount))).GroupBy(p => p.Method)
            .Select(g => new StoreReportRow
            {
                Key = ((int)g.Key).ToString(),
                Name = g.Key.ToString(),
                Count = g.Count(),
                Amount = R2(g.Sum(p => p.Amount)),
            }).OrderByDescending(r => r.Amount).ToList();
        // Cash is what stayed in the till: handed over minus the change given back.
        var cashRow = byPayment.FirstOrDefault(r => r.Key == ((int)StorePaymentMethod.Cash).ToString());
        if (cashRow is not null)
            cashRow.Amount = R2(cashRow.Amount - completed.Sum(h => h.ChangeAmount));

        var response = new StoreSalesReportResponse
        {
            From = q.From.Date,
            To = q.To.Date,
            SalesCount = completed.Count,
            GrossSales = R2(lines.Sum(l => l.Gross)),
            Discounts = R2(lines.Sum(l => l.Discount)),
            NetSales = R2(net),
            Vat = R2(lines.Sum(l => l.Vat)),
            CostOfGoods = R2(cost),
            AverageReceipt = completed.Count == 0 ? 0 : R2(completed.Sum(h => h.Total) / completed.Count),
            CancelledCount = cancelled.Count,
            CancelledTotal = R2(cancelled.Sum(h => h.Total)),
            ReturnsCount = filtered ? 0 : returns.Count,
            ReturnsTotal = R2(returnsTotal),
            ReturnedCost = R2(returnedCost),
            NetAfterReturns = R2(net - returnsTotal),
            Profit = R2(net - returnsTotal - (cost - returnedCost)),
            ByDay = Group(lines, l => Day(l.CreatedAtUtc), l => Day(l.CreatedAtUtc), l => l.Quantity, l => l.LineTotal, l => l.Cost)
                .OrderBy(r => r.Key).ToList(),
            ByProduct = Group(lines, l => l.ProductId.ToString(), l => l.ProductName, l => l.Quantity, l => l.LineTotal, l => l.Cost)
                .OrderByDescending(r => r.Amount).ToList(),
            ByCategory = Group(lines, l => (l.CategoryId ?? 0).ToString(), l => l.CategoryName ?? "Kateqoriyasız", l => l.Quantity, l => l.LineTotal, l => l.Cost)
                .OrderByDescending(r => r.Amount).ToList(),
            ByCashier = Group(lines, l => (l.CashierUserId ?? 0).ToString(),
                l => l.CashierUserId is { } id && names.TryGetValue(id, out var n) ? n : "—", l => l.Quantity, l => l.LineTotal, l => l.Cost)
                .OrderByDescending(r => r.Amount).ToList(),
            ByRegister = Group(lines, l => l.RegisterId.ToString(), l => l.RegisterName, l => l.Quantity, l => l.LineTotal, l => l.Cost)
                .OrderByDescending(r => r.Amount).ToList(),
            ByPayment = byPayment,
        };

        if (q.IncludeUnsold)
            response.Unsold = (await _reports.GetUnsoldProductsAsync(companyId, fromUtc, toUtc, q.WarehouseId, 100, ct))
                .Select(u => new StoreUnsoldProduct { ProductId = u.ProductId, ProductName = u.ProductName, CategoryName = u.CategoryName, Stock = u.Stock }).ToList();

        return response;
    }

    // ---- Grouped sales (1C style) -----------------------------------------------------------

    public async Task<StoreSalesMatrixResponse> Handle(GetStoreSalesMatrixQuery query, CancellationToken ct)
    {
        var q = query.Request;
        var groups = q.GroupBy.Select(g => g?.Trim() ?? "").Where(g => g.Length > 0).Distinct().ToList();
        if (groups.Any(g => !MatrixGroups.Contains(g)))
            throw new Application.Common.Exceptions.BadRequestException("Qruplaşdırma növü yanlışdır.");
        if (!string.IsNullOrWhiteSpace(q.SortBy) && !MatrixSorts.Contains(q.SortBy))
            throw new Application.Common.Exceptions.BadRequestException("Sıralama sahəsi yanlışdır.");

        var (fromUtc, toUtc) = Bounds(q.From, q.To);
        var branches = await _access.AllowedBranchesAsync(ct);
        var rows = await _reports.GetSaleDetailLinesAsync(_currentUser.CompanyId, fromUtc, toUtc, branches, NullIfEmpty(q.CustomerIds), NullIfEmpty(q.RegisterIds), NullIfEmpty(q.ProductIds), ct);
        if (NullIfEmpty(q.SupplierIds) is { } suppliers)
            rows = rows.Where(r => r.SupplierId != null && suppliers.Contains(r.SupplierId.Value)).ToList();

        string Day(DateTime utc) => (BusinessTime.FromUtc(utc) - ReportPeriodResolver.DayStart).Date.ToString("yyyy-MM-dd");

        (string Key, string Name) KeyOf(string group, SaleDetailRow r) => group switch
        {
            "day" => (Day(r.CreatedAtUtc), Day(r.CreatedAtUtc)),
            "customer" => ((r.CustomerId ?? 0).ToString(), r.CustomerName ?? "Müştərisiz"),
            "register" => (r.RegisterId.ToString(), r.RegisterName),
            "supplier" => ((r.SupplierId ?? 0).ToString(), r.SupplierName ?? "Tədarükçüsüz"),
            "product" => (r.ProductId.ToString(), r.ProductName),
            _ => (r.SaleId.ToString(), r.DocumentNo),
        };

        StoreSalesMatrixNode Node(string key, string name, string? group, List<SaleDetailRow> items)
        {
            var turnover = items.Sum(i => i.LineTotal);
            var cost = items.Sum(i => i.Cost);
            return new StoreSalesMatrixNode
            {
                Key = key,
                Name = name,
                Group = group,
                Quantity = items.Sum(i => i.Quantity),
                Turnover = R2(turnover),
                GrossTurnover = R2(items.Sum(i => i.Gross)),
                Cost = R2(cost),
                Profit = R2(turnover - cost),
                ProfitPercent = turnover == 0 ? 0 : R2((turnover - cost) / turnover * 100m),
            };
        }

        decimal Sort(StoreSalesMatrixNode n) => q.SortBy switch
        {
            "quantity" => n.Quantity,
            "turnover" => n.Turnover,
            "grossTurnover" => n.GrossTurnover,
            "cost" => n.Cost,
            "profit" => n.Profit,
            _ => n.ProfitPercent,
        };

        List<StoreSalesMatrixNode> Build(List<SaleDetailRow> items, int level)
        {
            if (level >= groups.Count) return new();
            var group = groups[level];
            var nodes = items.GroupBy(i => KeyOf(group, i)).Select(g =>
            {
                var node = Node(g.Key.Key, g.Key.Name, group, g.ToList());
                node.Children = Build(g.ToList(), level + 1);
                return node;
            });
            if (string.IsNullOrWhiteSpace(q.SortBy))
                return nodes.OrderBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            return (q.SortDesc ? nodes.OrderByDescending(Sort) : nodes.OrderBy(Sort)).ToList();
        }

        var total = Node("total", "Cəmi", null, rows);
        total.Children = Build(rows, 0);
        return new StoreSalesMatrixResponse { From = q.From.Date, To = q.To.Date, GroupBy = groups, Total = total };
    }

    private static List<int>? NullIfEmpty(List<int>? ids) => ids is { Count: > 0 } ? ids : null;

    // ---- Cash by till (opening, in, out, closing) -------------------------------------------

    public async Task<StoreCashSummaryResponse> Handle(GetStoreCashSummaryQuery q, CancellationToken ct)
    {
        var (fromUtc, toUtc) = Bounds(q.From, q.To);
        var branches = await _access.AllowedBranchesAsync(ct);
        var ledger = await _reports.GetCashLedgerAsync(_currentUser.CompanyId, toUtc, branches, ct);

        var rows = ledger.Registers.Select(r =>
        {
            var events = ledger.Events.Where(e => e.RegisterId == r.Id).ToList();
            var opening = events.Where(e => e.AtUtc < fromUtc).Sum(e => e.Amount);
            var income = events.Where(e => e.AtUtc >= fromUtc && e.Amount > 0).Sum(e => e.Amount);
            var outcome = -events.Where(e => e.AtUtc >= fromUtc && e.Amount < 0).Sum(e => e.Amount);
            return new StoreCashSummaryRow
            {
                RegisterId = r.Id,
                RegisterName = r.Name,
                Opening = R2(opening),
                Income = R2(income),
                Outcome = R2(outcome),
                Closing = R2(opening + income - outcome),
            };
        }).ToList();

        return new StoreCashSummaryResponse
        {
            From = q.From.Date,
            To = q.To.Date,
            Registers = rows,
            Total = new StoreCashSummaryRow
            {
                RegisterName = "Cəmi",
                Opening = rows.Sum(r => r.Opening),
                Income = rows.Sum(r => r.Income),
                Outcome = rows.Sum(r => r.Outcome),
                Closing = rows.Sum(r => r.Closing),
            },
        };
    }

    // ---- Product rating (most / least sold) -------------------------------------------------

    public async Task<StoreProductRatingResponse> Handle(GetStoreProductRatingQuery q, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var (fromUtc, toUtc) = Bounds(q.From, q.To);
        var branches = await _access.AllowedBranchesAsync(ct);
        var lines = await _reports.GetSaleDetailLinesAsync(companyId, fromUtc, toUtc, branches, null, null, null, ct);

        var grouped = lines.GroupBy(l => l.ProductId).Select(g => new
        {
            ProductId = g.Key,
            Name = g.First().ProductName,
            Quantity = g.Sum(l => l.Quantity),
            Amount = R2(g.Sum(l => l.LineTotal)),
        }).ToList();

        var ordered = (q.Least
                ? grouped.OrderBy(g => g.Quantity).ThenBy(g => g.Amount)
                : grouped.OrderByDescending(g => g.Quantity).ThenByDescending(g => g.Amount))
            .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(q.Take, 1, 1000))
            .ToList();

        var info = await _reports.GetProductInfoAsync(companyId, ordered.Select(o => o.ProductId).ToList(), ct);
        return new StoreProductRatingResponse
        {
            From = q.From.Date,
            To = q.To.Date,
            Least = q.Least,
            ProductCount = grouped.Count,
            TotalQuantity = grouped.Sum(g => g.Quantity),
            TotalAmount = R2(grouped.Sum(g => g.Amount)),
            Rows = ordered.Select((o, i) =>
            {
                info.TryGetValue(o.ProductId, out var p);
                return new StoreProductRatingRow
                {
                    Rank = i + 1,
                    ProductId = o.ProductId,
                    Sku = p?.Sku,
                    Barcode = p?.Barcode,
                    ProductName = o.Name,
                    SalePrice = p?.SalePrice ?? 0m,
                    Quantity = o.Quantity,
                    Amount = o.Amount,
                };
            }).ToList(),
        };
    }

    // ---- Stock by warehouse (opening, in, out, closing) --------------------------------------

    public async Task<StoreStockSummaryResponse> Handle(GetStoreStockSummaryQuery q, CancellationToken ct)
    {
        var (fromUtc, toUtc) = Bounds(q.From, q.To);
        if (q.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var allowed = await _access.AllowedWarehousesAsync(ct);
        var rows = await _reports.GetStockSummaryAsync(_currentUser.CompanyId, fromUtc, toUtc, q.WarehouseId, allowed?.ToList(), q.CategoryId, ct);

        StoreStockSummaryRowResponse Sum(IEnumerable<StoreStockSummaryRowResponse> items)
        {
            var list = items.ToList();
            return new StoreStockSummaryRowResponse
            {
                OpeningQty = list.Sum(r => r.OpeningQty), OpeningValue = R2(list.Sum(r => r.OpeningValue)),
                InQty = list.Sum(r => r.InQty), InValue = R2(list.Sum(r => r.InValue)),
                OutQty = list.Sum(r => r.OutQty), OutValue = R2(list.Sum(r => r.OutValue)),
                ClosingQty = list.Sum(r => r.ClosingQty), ClosingValue = R2(list.Sum(r => r.ClosingValue)),
                SaleAmount = R2(list.Sum(r => r.SaleAmount)),
            };
        }

        var warehouses = rows.GroupBy(r => r.WarehouseId).Select(g =>
        {
            var items = g.Select(r =>
            {
                var closingQty = r.OpeningQty + r.InQty - r.OutQty;
                return new StoreStockSummaryRowResponse
                {
                    ProductId = r.ProductId, ProductName = r.ProductName, Barcode = r.Barcode,
                    PurchasePrice = r.PurchasePrice, SalePrice = r.SalePrice,
                    OpeningQty = r.OpeningQty, OpeningValue = R2(r.OpeningValue),
                    InQty = r.InQty, InValue = R2(r.InValue),
                    OutQty = r.OutQty, OutValue = R2(r.OutValue),
                    ClosingQty = closingQty, ClosingValue = R2(r.OpeningValue + r.InValue - r.OutValue),
                    SaleAmount = R2(closingQty * r.SalePrice),
                };
            }).OrderBy(r => r.ProductName, StringComparer.CurrentCultureIgnoreCase).ToList();
            return new StoreStockSummaryWarehouse { WarehouseId = g.Key, WarehouseName = g.First().WarehouseName, Rows = items, Total = Sum(items) };
        }).OrderBy(w => w.WarehouseName, StringComparer.CurrentCultureIgnoreCase).ToList();

        return new StoreStockSummaryResponse
        {
            From = q.From.Date, To = q.To.Date, Warehouses = warehouses,
            Total = Sum(warehouses.SelectMany(w => w.Rows)),
        };
    }

    // ---- Stock ------------------------------------------------------------------------------

    public async Task<StoreStockReportResponse> Handle(GetStoreStockReportQuery q, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var (fromUtc, toUtc) = Bounds(q.From, q.To);

        if (q.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var balances = await _store.SearchBalancesAsync(companyId, q.WarehouseId, null, false, ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowedWarehouses)
            balances = balances.Where(b => allowedWarehouses.Contains(b.WarehouseId)).ToList();
        if (q.CategoryId is { } categoryId)
            balances = balances.Where(b => b.StoreProduct.CategoryId == categoryId).ToList();

        StoreStockBalanceResponse Map(Domain.Entities.StoreMode.StoreStockBalance b) => new()
        {
            WarehouseId = b.WarehouseId,
            WarehouseName = b.Warehouse?.Name ?? string.Empty,
            ProductId = b.StoreProductId,
            ProductName = b.StoreProduct.Name,
            Sku = b.StoreProduct.Sku,
            PrimaryBarcode = b.StoreProduct.Barcodes.OrderByDescending(x => x.IsPrimary).Select(x => x.Barcode).FirstOrDefault(),
            Unit = b.StoreProduct.Unit,
            Quantity = b.Quantity,
            MinimumStock = b.StoreProduct.MinimumStock,
            IsLow = b.Quantity <= b.StoreProduct.MinimumStock,
            AverageCost = b.StoreProduct.AverageCost,
            Value = R2(b.Quantity * b.StoreProduct.AverageCost),
        };

        var rows = balances.Select(Map).ToList();
        var movements = await _reports.GetMovementsAsync(companyId, fromUtc, toUtc, q.WarehouseId, ct);

        return new StoreStockReportResponse
        {
            TotalValue = R2(rows.Sum(r => r.Value)),
            LowStockCount = rows.Count(r => r.IsLow && r.MinimumStock > 0),
            Rows = rows,
            BelowMinimum = rows.Where(r => r.IsLow && r.MinimumStock > 0).OrderBy(r => r.Quantity - r.MinimumStock).ToList(),
            ByWarehouse = rows.GroupBy(r => r.WarehouseId).Select(g => new StoreReportRow
            {
                Key = g.Key.ToString(),
                Name = g.First().WarehouseName,
                Count = g.Count(),
                Quantity = g.Sum(r => r.Quantity),
                Amount = R2(g.Sum(r => r.Value)),
            }).OrderByDescending(r => r.Amount).ToList(),
            MovementsByType = movements.GroupBy(m => m.Type).Select(g => new StoreReportRow
            {
                Key = ((int)g.Key).ToString(),
                Name = g.Key.ToString(),
                Count = g.Count(),
                Quantity = g.Sum(m => m.Quantity),
                Amount = R2(g.Sum(m => m.Quantity * m.UnitCost)),
            }).OrderBy(r => r.Key).ToList(),
        };
    }

    // ---- Cash -------------------------------------------------------------------------------

    public async Task<StoreCashReportResponse> Handle(GetStoreCashReportQuery q, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var (fromUtc, toUtc) = Bounds(q.From, q.To);

        var branches = await _access.AllowedBranchesAsync(ct);
        var movements = await _reports.GetCashMovementsAsync(companyId, fromUtc, toUtc, q.RestaurantId, ct, branches);
        var shifts = await _store.SearchShiftsAsync(companyId, null, fromUtc, toUtc, ct);
        var names = await _store.GetUserNamesAsync(
            movements.Select(m => m.UserId).Concat(shifts.Select(s => (int?)s.CashierUserId)).OfType<int>(), ct);

        return new StoreCashReportResponse
        {
            TotalIn = movements.Where(m => m.Type == StoreCashMovementType.In).Sum(m => m.Amount),
            TotalOut = movements.Where(m => m.Type == StoreCashMovementType.Out).Sum(m => m.Amount),
            TotalDifference = shifts.Sum(s => s.CashDifference ?? 0m),
            Movements = movements.Select(m => new StoreCashReportMovement
            {
                Id = m.Id,
                CreatedAtUtc = m.CreatedAtUtc,
                RegisterName = m.RegisterName,
                Type = m.Type,
                Amount = m.Amount,
                Reason = m.Reason,
                UserName = m.UserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            }).ToList(),
            Shifts = shifts.Where(s => (q.RestaurantId == null || s.RestaurantId == q.RestaurantId) && (branches == null || branches.Contains(s.RestaurantId))).Select(s => new StoreCashReportShift
            {
                ShiftId = s.Id,
                RegisterName = s.StoreCashRegister?.Name ?? string.Empty,
                CashierName = names.GetValueOrDefault(s.CashierUserId),
                OpenedAtUtc = s.OpenedAtUtc,
                ClosedAtUtc = s.ClosedAtUtc,
                ExpectedCash = s.ExpectedCash,
                ActualCash = s.ActualCash,
                CashDifference = s.CashDifference,
            }).ToList(),
        };
    }

    // ---- Customer debts and aging -----------------------------------------------------------

    public async Task<StoreCustomerDebtReportResponse> Handle(GetStoreCustomerDebtReportQuery q, CancellationToken ct)
    {
        var entries = await _reports.GetCustomerLedgerAsync(_currentUser.CompanyId, ct);
        var today = DateTime.UtcNow.Date;
        var rows = new List<StoreCustomerDebtRow>();

        foreach (var customer in entries.GroupBy(e => e.CustomerId))
        {
            // Payments and credits settle the oldest debts first; what is left is aged by when it arose.
            var open = new List<(DateTime Date, decimal Amount)>();
            foreach (var e in customer)
            {
                if (e.Amount > 0)
                {
                    open.Add((e.EntryDate, e.Amount));
                    continue;
                }

                var pay = -e.Amount;
                for (var i = 0; i < open.Count && pay > 0; i++)
                {
                    var used = Math.Min(open[i].Amount, pay);
                    open[i] = (open[i].Date, open[i].Amount - used);
                    pay -= used;
                }
            }

            open = open.Where(o => o.Amount > 0).ToList();
            var debt = open.Sum(o => o.Amount);
            if (debt <= 0)
                continue;

            var row = new StoreCustomerDebtRow
            {
                CustomerId = customer.Key,
                CustomerName = customer.First().CustomerName,
                Debt = R2(debt),
                OldestOpenDate = open.Min(o => o.Date),
            };
            foreach (var (date, amount) in open)
            {
                var age = (today - date.Date).Days;
                if (age <= 30) row.Age0To30 += amount;
                else if (age <= 60) row.Age31To60 += amount;
                else if (age <= 90) row.Age61To90 += amount;
                else row.Age90Plus += amount;
            }
            rows.Add(row);
        }

        return new StoreCustomerDebtReportResponse
        {
            TotalDebt = R2(rows.Sum(r => r.Debt)),
            TotalOver30 = R2(rows.Sum(r => r.Age31To60 + r.Age61To90 + r.Age90Plus)),
            Customers = rows.OrderByDescending(r => r.Age90Plus + r.Age61To90).ThenByDescending(r => r.Debt).ToList(),
        };
    }

    // ---- Bonus ------------------------------------------------------------------------------

    /// <summary>The bonus customers: everyone who had a bonus balance, a bonus movement in the period, or has a balance now.</summary>
    public async Task<StoreBonusReportResponse> Handle(GetStoreBonusReportQuery q, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var (fromUtc, toUtc) = Bounds(q.From, q.To);
        var entries = await _reports.GetBonusEntriesAsync(companyId, fromUtc, toUtc, ct);
        var opening = (await _reports.GetBonusBalancesAsync(companyId, fromUtc, ct)).ToDictionary(b => b.CustomerId);
        var current = (await _reports.GetBonusBalancesAsync(companyId, null, ct)).ToDictionary(b => b.CustomerId);

        var byCustomer = entries.GroupBy(e => e.CustomerId).ToDictionary(g => g.Key, g => g.ToList());
        var ids = byCustomer.Keys
            .Concat(opening.Where(o => o.Value.Amount != 0).Select(o => o.Key))
            .Concat(current.Where(o => o.Value.Amount != 0).Select(o => o.Key))
            .Distinct();

        var rows = ids.Select(id =>
        {
            var g = byCustomer.GetValueOrDefault(id) ?? new List<BonusRow>();
            var open = opening.TryGetValue(id, out var o) ? o.Amount : 0m;
            var net = g.Sum(e => e.Amount);
            var name = g.FirstOrDefault()?.CustomerName ?? o?.CustomerName ?? current[id].CustomerName;
            return new StoreBonusReportRow
            {
                CustomerId = id,
                CustomerName = name,
                Earned = g.Where(e => e.Type == StoreBonusEntryType.Earned).Sum(e => e.Amount),
                Spent = -g.Where(e => e.Type == StoreBonusEntryType.Spent).Sum(e => e.Amount),
                Reversed = -g.Where(e => e.Type == StoreBonusEntryType.Reversal).Sum(e => e.Amount),
                Refunded = g.Where(e => e.Type == StoreBonusEntryType.Refund).Sum(e => e.Amount),
                Adjusted = g.Where(e => e.Type == StoreBonusEntryType.Adjustment).Sum(e => e.Amount),
                Expired = -g.Where(e => e.Type == StoreBonusEntryType.Expired).Sum(e => e.Amount),
                Net = net,
                Opening = open,
                Increase = g.Where(e => e.Amount > 0).Sum(e => e.Amount),
                Decrease = -g.Where(e => e.Amount < 0).Sum(e => e.Amount),
                Closing = open + net,
                Current = current.TryGetValue(id, out var c) ? c.Amount : 0m,
            };
        }).OrderBy(r => r.CustomerName, StringComparer.CurrentCultureIgnoreCase).ToList();

        return new StoreBonusReportResponse
        {
            TotalEarned = rows.Sum(r => r.Earned),
            TotalSpent = rows.Sum(r => r.Spent),
            TotalReversed = rows.Sum(r => r.Reversed),
            Customers = rows,
        };
    }
}
