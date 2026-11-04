using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreReportRepository : IStoreReportRepository
{
    private readonly AppDbContext _context;

    public StoreReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SaleLineRow>> GetSaleLinesAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId,
        int? cashierUserId, int? categoryId, int? productId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null)
    {
        var rows = await _context.StoreSaleLines.AsNoTracking()
            .Where(l => l.StoreSale.CompanyId == companyId
                && l.StoreSale.Status == StoreSaleStatus.Completed
                && l.StoreSale.CreatedAtUtc >= fromUtc && l.StoreSale.CreatedAtUtc < toUtcExclusive
                && (restaurantId == null || l.StoreSale.RestaurantId == restaurantId)
                && (branchIds == null || branchIds.Contains(l.StoreSale.RestaurantId))
                && (warehouseId == null || l.StoreSale.WarehouseId == warehouseId)
                && (cashierUserId == null || l.StoreSale.CreatedByUserId == cashierUserId)
                && (categoryId == null || l.StoreProduct.CategoryId == categoryId)
                && (productId == null || l.StoreProductId == productId))
            .Select(l => new
            {
                l.StoreSaleId,
                l.StoreSale.CreatedAtUtc,
                l.StoreSale.CreatedByUserId,
                l.StoreSale.StoreCashRegisterId,
                RegisterName = l.StoreSale.StoreCashRegister.Name,
                l.StoreSale.RestaurantId,
                l.StoreSale.WarehouseId,
                l.StoreProductId,
                l.ProductName,
                l.StoreProduct.CategoryId,
                CategoryName = l.StoreProduct.Category != null ? l.StoreProduct.Category.Name : null,
                l.Quantity,
                l.UnitPrice,
                l.DiscountAmount,
                l.LineTotal,
                l.VatAmount,
                l.UnitCost,
            })
            .ToListAsync(ct);

        return rows.Select(r => new SaleLineRow(
            r.StoreSaleId, r.CreatedAtUtc, r.CreatedByUserId, r.StoreCashRegisterId, r.RegisterName, r.RestaurantId, r.WarehouseId,
            r.StoreProductId, r.ProductName, r.CategoryId, r.CategoryName,
            r.Quantity, Math.Round(r.Quantity * r.UnitPrice, 2), r.DiscountAmount, r.LineTotal, r.VatAmount,
            Math.Round(r.Quantity * r.UnitCost, 2))).ToList();
    }

    public async Task<List<SaleDetailRow>> GetSaleDetailLinesAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<int>? branchIds,
        IReadOnlyCollection<int>? customerIds, IReadOnlyCollection<int>? registerIds, IReadOnlyCollection<int>? productIds, CancellationToken ct)
    {
        var rows = await _context.StoreSaleLines.AsNoTracking()
            .Where(l => l.StoreSale.CompanyId == companyId
                && l.StoreSale.Status == StoreSaleStatus.Completed
                && l.StoreSale.CreatedAtUtc >= fromUtc && l.StoreSale.CreatedAtUtc < toUtcExclusive
                && (branchIds == null || branchIds.Contains(l.StoreSale.RestaurantId))
                && (customerIds == null || (l.StoreSale.StoreCustomerId != null && customerIds.Contains(l.StoreSale.StoreCustomerId.Value)))
                && (registerIds == null || registerIds.Contains(l.StoreSale.StoreCashRegisterId))
                && (productIds == null || productIds.Contains(l.StoreProductId)))
            .Select(l => new
            {
                l.StoreSaleId,
                l.StoreSale.DocumentNo,
                l.StoreSale.CreatedAtUtc,
                l.StoreSale.StoreCustomerId,
                CustomerName = l.StoreSale.StoreCustomer != null ? l.StoreSale.StoreCustomer.Name : null,
                l.StoreSale.StoreCashRegisterId,
                RegisterName = l.StoreSale.StoreCashRegister.Name,
                l.StoreProductId,
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.LineTotal,
                l.UnitCost,
            })
            .ToListAsync(ct);

        // A product has no fixed supplier: it is the one of the latest confirmed purchase that brought it in.
        var productIdsInRows = rows.Select(r => r.StoreProductId).Distinct().ToList();
        var lastSuppliers = (await _context.StorePurchaseLines.AsNoTracking()
                .Where(l => productIdsInRows.Contains(l.StoreProductId)
                    && l.StorePurchase.CompanyId == companyId
                    && l.StorePurchase.Status == StorePurchaseStatus.Confirmed)
                .Select(l => new { l.StoreProductId, l.StorePurchase.StoreSupplierId, SupplierName = l.StorePurchase.StoreSupplier.Name, l.StorePurchase.PurchaseDate, PurchaseId = l.StorePurchaseId })
                .ToListAsync(ct))
            .GroupBy(x => x.StoreProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.PurchaseDate).ThenByDescending(x => x.PurchaseId).First());

        return rows.Select(r =>
        {
            lastSuppliers.TryGetValue(r.StoreProductId, out var s);
            return new SaleDetailRow(
                r.StoreSaleId, r.DocumentNo, r.CreatedAtUtc, r.StoreCustomerId, r.CustomerName, r.StoreCashRegisterId, r.RegisterName,
                r.StoreProductId, r.ProductName, s?.StoreSupplierId, s?.SupplierName,
                r.Quantity, Math.Round(r.Quantity * r.UnitPrice, 2), r.LineTotal, Math.Round(r.Quantity * r.UnitCost, 2));
        }).ToList();
    }

    public async Task<List<SaleHeaderRow>> GetSaleHeadersAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId, int? cashierUserId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null)
    {
        var rows = await _context.StoreSales.AsNoTracking()
            .Include(s => s.Payments)
            .Where(s => s.CompanyId == companyId
                && s.CreatedAtUtc >= fromUtc && s.CreatedAtUtc < toUtcExclusive
                && (restaurantId == null || s.RestaurantId == restaurantId)
                && (branchIds == null || branchIds.Contains(s.RestaurantId))
                && (warehouseId == null || s.WarehouseId == warehouseId)
                && (cashierUserId == null || s.CreatedByUserId == cashierUserId))
            .ToListAsync(ct);

        return rows.Select(s => new SaleHeaderRow(
            s.Id, s.CreatedAtUtc, s.Status, s.Total, s.CreatedByUserId, s.StoreCashRegisterId,
            s.Payments.Select(p => (p.Method, p.Amount)).ToList(), s.ChangeAmount)).ToList();
    }

    public async Task<List<ReturnRow>> GetReturnsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null)
    {
        var rows = await _context.StoreSaleReturns.AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId
                && r.CreatedAtUtc >= fromUtc && r.CreatedAtUtc < toUtcExclusive
                && (restaurantId == null || r.RestaurantId == restaurantId)
                && (branchIds == null || branchIds.Contains(r.RestaurantId))
                && (warehouseId == null || r.WarehouseId == warehouseId))
            .ToListAsync(ct);

        return rows.Select(r => new ReturnRow(
            r.Id, r.CreatedAtUtc, r.TotalAmount,
            r.Lines.Where(l => l.Condition == StoreReturnCondition.Sellable).Sum(l => Math.Round(l.Quantity * l.UnitCost, 2)),
            r.CreatedByUserId)).ToList();
    }

    public Task<List<CashMovementRow>> GetCashMovementsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null)
        => _context.StoreCashMovements.AsNoTracking()
            .Where(m => m.CompanyId == companyId
                && m.CreatedAtUtc >= fromUtc && m.CreatedAtUtc < toUtcExclusive
                && (restaurantId == null || m.StoreCashShift.RestaurantId == restaurantId)
                && (branchIds == null || branchIds.Contains(m.StoreCashShift.RestaurantId)))
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new CashMovementRow(
                m.Id, m.CreatedAtUtc, m.StoreCashShiftId, m.StoreCashShift.StoreCashRegister.Name, m.Type, m.Amount, m.Reason, m.CreatedByUserId))
            .ToListAsync(ct);

    public Task<List<MovementRow>> GetMovementsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, CancellationToken ct)
        => _context.StoreStockMovements.AsNoTracking()
            .Where(m => m.CompanyId == companyId
                && m.CreatedAtUtc >= fromUtc && m.CreatedAtUtc < toUtcExclusive
                && (warehouseId == null || m.WarehouseId == warehouseId))
            .Select(m => new MovementRow(m.Type, m.WarehouseId, m.Warehouse.Name, m.StoreProductId, m.Quantity, m.UnitCost, m.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task<List<UnsoldProductRow>> GetUnsoldProductsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, int take, CancellationToken ct)
    {
        var soldIds = _context.StoreSaleLines
            .Where(l => l.StoreSale.CompanyId == companyId && l.StoreSale.Status == StoreSaleStatus.Completed
                && l.StoreSale.CreatedAtUtc >= fromUtc && l.StoreSale.CreatedAtUtc < toUtcExclusive)
            .Select(l => l.StoreProductId);

        var rows = await _context.StoreProducts.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.IsActive && !soldIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.Name,
                CategoryName = p.Category != null ? p.Category.Name : null,
                Stock = _context.StoreStockBalances
                    .Where(b => b.StoreProductId == p.Id && (warehouseId == null || b.WarehouseId == warehouseId))
                    .Sum(b => (decimal?)b.Quantity) ?? 0m,
            })
            .ToListAsync(ct);

        return rows.Select(r => new UnsoldProductRow(r.Id, r.Name, r.CategoryName, r.Stock)).ToList();
    }

    public async Task<List<CreditSaleRow>> GetCustomerLedgerAsync(int companyId, CancellationToken ct)
    {
        var entries = await _context.StoreLedgerEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.PartyType == StoreLedgerPartyType.Customer)
            .OrderBy(e => e.EntryDate).ThenBy(e => e.Id)
            .ToListAsync(ct);
        var names = await _context.StoreCustomers.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return entries.Select(e => new CreditSaleRow(e.PartyId, names.GetValueOrDefault(e.PartyId, "—"), e.Type, e.Amount, e.EntryDate)).ToList();
    }

    public async Task<List<StockSummaryRow>> GetStockSummaryAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, IReadOnlyCollection<int>? warehouseIds, int? categoryId, CancellationToken ct)
    {
        var grouped = await _context.StoreStockMovements.AsNoTracking()
            .Where(m => m.CompanyId == companyId
                && m.CreatedAtUtc < toUtcExclusive
                && (warehouseId == null || m.WarehouseId == warehouseId)
                && (warehouseIds == null || warehouseIds.Contains(m.WarehouseId))
                && (categoryId == null || m.StoreProduct.CategoryId == categoryId))
            .GroupBy(m => new { m.WarehouseId, m.StoreProductId })
            .Select(g => new
            {
                g.Key.WarehouseId,
                g.Key.StoreProductId,
                OpeningQty = g.Sum(m => m.CreatedAtUtc < fromUtc ? m.Quantity : 0m),
                OpeningValue = g.Sum(m => m.CreatedAtUtc < fromUtc ? m.Quantity * m.UnitCost : 0m),
                InQty = g.Sum(m => m.CreatedAtUtc >= fromUtc && m.Quantity > 0 ? m.Quantity : 0m),
                InValue = g.Sum(m => m.CreatedAtUtc >= fromUtc && m.Quantity > 0 ? m.Quantity * m.UnitCost : 0m),
                OutQty = g.Sum(m => m.CreatedAtUtc >= fromUtc && m.Quantity < 0 ? -m.Quantity : 0m),
                OutValue = g.Sum(m => m.CreatedAtUtc >= fromUtc && m.Quantity < 0 ? -m.Quantity * m.UnitCost : 0m),
            })
            .ToListAsync(ct);
        grouped = grouped.Where(g => g.OpeningQty != 0 || g.InQty != 0 || g.OutQty != 0).ToList();
        if (grouped.Count == 0) return new();

        var productIds = grouped.Select(g => g.StoreProductId).Distinct().ToList();
        var products = await _context.StoreProducts.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.PurchasePrice,
                p.SalePrice,
                Barcode = p.Barcodes.OrderByDescending(b => b.IsPrimary).Select(b => b.Barcode).FirstOrDefault(),
            })
            .ToDictionaryAsync(p => p.Id, ct);
        var warehouses = await _context.Warehouses.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        return grouped.Select(g =>
        {
            var p = products[g.StoreProductId];
            return new StockSummaryRow(
                g.WarehouseId, warehouses.GetValueOrDefault(g.WarehouseId, "—"), g.StoreProductId, p.Name, p.Barcode, p.PurchasePrice, p.SalePrice,
                g.OpeningQty, g.OpeningValue, g.InQty, g.InValue, g.OutQty, g.OutValue);
        }).ToList();
    }

    public async Task<Dictionary<int, ProductInfoRow>> GetProductInfoAsync(int companyId, IReadOnlyCollection<int> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0) return new();
        var rows = await _context.StoreProducts.AsNoTracking()
            .Where(p => p.CompanyId == companyId && productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.SalePrice,
                Barcode = p.Barcodes.OrderByDescending(b => b.IsPrimary).Select(b => b.Barcode).FirstOrDefault(),
            })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => new ProductInfoRow(r.Id, r.Sku, r.Barcode, r.SalePrice));
    }

    public async Task<CashLedger> GetCashLedgerAsync(int companyId, DateTime toUtcExclusive, IReadOnlyCollection<int>? branchIds, CancellationToken ct)
    {
        var registers = await _context.StoreCashRegisters.AsNoTracking()
            .Where(r => r.CompanyId == companyId && (branchIds == null || branchIds.Contains(r.RestaurantId)))
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);
        var registerIds = registers.Select(r => r.Id).ToList();
        var events = new List<CashEventRow>();

        var sales = await _context.StoreSales.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Status == StoreSaleStatus.Completed && s.CreatedAtUtc < toUtcExclusive
                && registerIds.Contains(s.StoreCashRegisterId))
            .Select(s => new
            {
                s.StoreCashRegisterId,
                s.CreatedAtUtc,
                Cash = s.Payments.Where(p => p.Method == StorePaymentMethod.Cash).Sum(p => p.Amount) - s.ChangeAmount,
            })
            .ToListAsync(ct);
        events.AddRange(sales.Where(s => s.Cash != 0).Select(s => new CashEventRow(s.StoreCashRegisterId, s.CreatedAtUtc, s.Cash)));

        var customerPayments = await _context.StoreCustomerPayments.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.ReversedAtUtc == null && p.Method == StorePaymentMethod.Cash && p.CreatedAtUtc < toUtcExclusive
                && registerIds.Contains(p.StoreCashShift.StoreCashRegisterId))
            .Select(p => new { p.StoreCashShift.StoreCashRegisterId, p.CreatedAtUtc, p.Amount })
            .ToListAsync(ct);
        events.AddRange(customerPayments.Select(p => new CashEventRow(p.StoreCashRegisterId, p.CreatedAtUtc, p.Amount)));

        var movements = await _context.StoreCashMovements.AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.CreatedAtUtc < toUtcExclusive && registerIds.Contains(m.StoreCashShift.StoreCashRegisterId))
            .Select(m => new { m.StoreCashShift.StoreCashRegisterId, m.CreatedAtUtc, m.Type, m.Amount })
            .ToListAsync(ct);
        events.AddRange(movements.Select(m => new CashEventRow(m.StoreCashRegisterId, m.CreatedAtUtc, m.Type == StoreCashMovementType.In ? m.Amount : -m.Amount)));

        var refunds = await _context.StoreSaleReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RefundMethod == StorePaymentMethod.Cash && r.CreatedAtUtc < toUtcExclusive
                && registerIds.Contains(r.StoreCashRegisterId))
            .Select(r => new { r.StoreCashRegisterId, r.CreatedAtUtc, r.RefundAmount })
            .ToListAsync(ct);
        events.AddRange(refunds.Select(r => new CashEventRow(r.StoreCashRegisterId, r.CreatedAtUtc, -r.RefundAmount)));

        // The float counted into a till when its very first shift opened is its starting money.
        var firstShifts = (await _context.StoreCashShifts.AsNoTracking()
                .Where(s => s.CompanyId == companyId && registerIds.Contains(s.StoreCashRegisterId) && s.OpenedAtUtc < toUtcExclusive)
                .Select(s => new { s.StoreCashRegisterId, s.OpenedAtUtc, s.OpeningCash })
                .ToListAsync(ct))
            .GroupBy(s => s.StoreCashRegisterId)
            .Select(g => g.OrderBy(s => s.OpenedAtUtc).First());
        events.AddRange(firstShifts.Where(s => s.OpeningCash != 0).Select(s => new CashEventRow(s.StoreCashRegisterId, s.OpenedAtUtc, s.OpeningCash)));

        return new CashLedger(registers.Select(r => (r.Id, r.Name)).ToList(), events);
    }

    public async Task<List<BonusRow>> GetBonusBalancesAsync(int companyId, DateTime? beforeUtc, CancellationToken ct)
    {
        var rows = await _context.StoreBonusEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && (beforeUtc == null || e.CreatedAtUtc < beforeUtc))
            .Select(e => new { e.StoreCustomerId, e.StoreCustomer.Name, e.Amount })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.StoreCustomerId)
            .Select(g => new BonusRow(g.Key, g.First().Name, StoreBonusEntryType.Adjustment, g.Sum(r => r.Amount))).ToList();
    }

    public async Task<List<BonusRow>> GetBonusEntriesAsync(int companyId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct)
    {
        var rows = await _context.StoreBonusEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.CreatedAtUtc >= fromUtc && e.CreatedAtUtc < toUtcExclusive)
            .Select(e => new { e.StoreCustomerId, e.StoreCustomer.Name, e.Type, e.Amount })
            .ToListAsync(ct);
        return rows.Select(r => new BonusRow(r.StoreCustomerId, r.Name, r.Type, r.Amount)).ToList();
    }
}
