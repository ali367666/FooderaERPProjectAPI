using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Flat, read-only rows for the store reports; the report handlers group and total them.</summary>
public interface IStoreReportRepository
{
    Task<List<SaleLineRow>> GetSaleLinesAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId,
        int? cashierUserId, int? categoryId, int? productId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null);

    /// <summary>Every completed sale line of the period with its customer, till, document and (last purchase) supplier, for the grouped sales report.</summary>
    Task<List<SaleDetailRow>> GetSaleDetailLinesAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<int>? branchIds,
        IReadOnlyCollection<int>? customerIds, IReadOnlyCollection<int>? registerIds, IReadOnlyCollection<int>? productIds, CancellationToken ct);

    Task<List<SaleHeaderRow>> GetSaleHeadersAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId, int? cashierUserId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null);

    Task<List<ReturnRow>> GetReturnsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, int? warehouseId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null);

    Task<List<CashMovementRow>> GetCashMovementsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? restaurantId, CancellationToken ct, IReadOnlyCollection<int>? branchIds = null);

    Task<List<MovementRow>> GetMovementsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, CancellationToken ct);

    /// <summary>Products that exist, are active and have no completed sale in the period.</summary>
    Task<List<UnsoldProductRow>> GetUnsoldProductsAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, int take, CancellationToken ct);

    Task<List<CreditSaleRow>> GetCustomerLedgerAsync(int companyId, CancellationToken ct);

    /// <summary>Each customer's bonus balance from all entries made before the given moment (all entries when null).</summary>
    /// <summary>Per warehouse and product: stock before the period, what came in and went out during it (quantity and cost value), from the movement ledger.</summary>
    Task<List<StockSummaryRow>> GetStockSummaryAsync(
        int companyId, DateTime fromUtc, DateTime toUtcExclusive, int? warehouseId, IReadOnlyCollection<int>? warehouseIds, int? categoryId, CancellationToken ct);

    /// <summary>Code, barcode and current sale price of the given products.</summary>
    Task<Dictionary<int, ProductInfoRow>> GetProductInfoAsync(int companyId, IReadOnlyCollection<int> productIds, CancellationToken ct);

    /// <summary>
    /// Every cash event of the tills up to the given moment: cash from sales (less change), customer cash payments, manual cash in/out,
    /// cash refunds and each till's first shift float. Amounts are signed (money in positive, money out negative).
    /// </summary>
    Task<CashLedger> GetCashLedgerAsync(int companyId, DateTime toUtcExclusive, IReadOnlyCollection<int>? branchIds, CancellationToken ct);

    Task<List<BonusRow>> GetBonusBalancesAsync(int companyId, DateTime? beforeUtc, CancellationToken ct);

    Task<List<BonusRow>> GetBonusEntriesAsync(int companyId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct);
}

public record SaleLineRow(
    int SaleId, DateTime CreatedAtUtc, int? CashierUserId, int RegisterId, string RegisterName, int RestaurantId, int WarehouseId,
    int ProductId, string ProductName, int? CategoryId, string? CategoryName,
    decimal Quantity, decimal Gross, decimal Discount, decimal LineTotal, decimal Vat, decimal Cost);

public record SaleDetailRow(
    int SaleId, string DocumentNo, DateTime CreatedAtUtc, int? CustomerId, string? CustomerName, int RegisterId, string RegisterName,
    int ProductId, string ProductName, int? SupplierId, string? SupplierName,
    decimal Quantity, decimal Gross, decimal LineTotal, decimal Cost);

public record StockSummaryRow(
    int WarehouseId, string WarehouseName, int ProductId, string ProductName, string? Barcode, decimal PurchasePrice, decimal SalePrice,
    decimal OpeningQty, decimal OpeningValue, decimal InQty, decimal InValue, decimal OutQty, decimal OutValue);

public record ProductInfoRow(int ProductId, string? Sku, string? Barcode, decimal SalePrice);

public record CashEventRow(int RegisterId, DateTime AtUtc, decimal Amount);

public record CashLedger(List<(int Id, string Name)> Registers, List<CashEventRow> Events);

public record SaleHeaderRow(
    int SaleId, DateTime CreatedAtUtc, StoreSaleStatus Status, decimal Total, int? CashierUserId, int RegisterId,
    List<(StorePaymentMethod Method, decimal Amount)> Payments, decimal ChangeAmount);

public record ReturnRow(int ReturnId, DateTime CreatedAtUtc, decimal Total, decimal SellableCost, int? CashierUserId);

public record CashMovementRow(int Id, DateTime CreatedAtUtc, int ShiftId, string RegisterName, StoreCashMovementType Type, decimal Amount, string Reason, int? UserId);

public record MovementRow(
    StoreStockMovementType Type, int WarehouseId, string WarehouseName, int ProductId, decimal Quantity, decimal UnitCost, DateTime CreatedAtUtc);

public record UnsoldProductRow(int ProductId, string ProductName, string? CategoryName, decimal Stock);

public record CreditSaleRow(int CustomerId, string CustomerName, StoreLedgerEntryType Type, decimal Amount, DateTime EntryDate);

public record BonusRow(int CustomerId, string CustomerName, StoreBonusEntryType Type, decimal Amount);
