using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    // ---- Catalog ----------------------------------------------------------------------------

    public Task<List<StoreCategory>> GetCategoriesAsync(int companyId, CancellationToken ct)
        => _context.StoreCategories.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<StoreCategory?> GetCategoryAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreCategories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddCategoryAsync(StoreCategory category, CancellationToken ct)
        => await _context.StoreCategories.AddAsync(category, ct);

    public Task<StoreProduct?> GetProductAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreProducts
            .Include(x => x.Barcodes)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreProduct>> GetProductsByIdsAsync(int companyId, IReadOnlyCollection<int> ids, CancellationToken ct)
        => _context.StoreProducts
            .Include(x => x.Barcodes)
            .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
            .ToListAsync(ct);

    public async Task<StoreProduct?> FindProductByCodeAsync(int companyId, string code, CancellationToken ct)
    {
        var byBarcode = await _context.StoreProductBarcodes.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.Barcode == code)
            .Select(b => b.StoreProductId)
            .FirstOrDefaultAsync(ct);

        return await _context.StoreProducts
            .Include(x => x.Barcodes)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive
                && ((byBarcode != 0 && x.Id == byBarcode) || x.Sku == code), ct);
    }

    public async Task<(List<StoreProduct> Items, int Total)> SearchProductsAsync(
        int companyId, string? term, int? categoryId, bool? isActive, int skip, int take, CancellationToken ct)
    {
        var query = _context.StoreProducts.AsNoTracking()
            .Include(x => x.Barcodes)
            .Include(x => x.Category)
            .Where(x => x.CompanyId == companyId);

        if (isActive is { } active)
            query = query.Where(x => x.IsActive == active);
        if (categoryId is { } category)
            query = query.Where(x => x.CategoryId == category);

        // The name matches anywhere in it; the SQL collation ignores case.
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(x => x.Name.Contains(t)
                || (x.Sku != null && x.Sku.Contains(t))
                || x.Barcodes.Any(b => b.Barcode.Contains(t)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<bool> BarcodeTakenAsync(int companyId, string barcode, int? exceptProductId, CancellationToken ct)
        => _context.StoreProductBarcodes.AnyAsync(
            b => b.CompanyId == companyId && b.Barcode == barcode && b.StoreProductId != exceptProductId, ct);

    public Task<bool> SkuTakenAsync(int companyId, string sku, int? exceptProductId, CancellationToken ct)
        => _context.StoreProducts.AnyAsync(
            x => x.CompanyId == companyId && x.Sku == sku && x.Id != exceptProductId, ct);

    public async Task AddProductAsync(StoreProduct product, CancellationToken ct)
        => await _context.StoreProducts.AddAsync(product, ct);

    // ---- Stock ------------------------------------------------------------------------------

    public async Task<StoreSettings> GetOrCreateSettingsAsync(int companyId, CancellationToken ct)
    {
        var settings = await _context.StoreSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
        if (settings is null)
        {
            settings = new StoreSettings { CompanyId = companyId };
            await _context.StoreSettings.AddAsync(settings, ct);
        }

        return settings;
    }

    public async Task<StoreStockBalance> GetOrCreateBalanceAsync(int companyId, int warehouseId, int productId, CancellationToken ct)
    {
        var balance = await _context.StoreStockBalances
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.StoreProductId == productId, ct);
        if (balance is null)
        {
            balance = new StoreStockBalance { CompanyId = companyId, WarehouseId = warehouseId, StoreProductId = productId };
            await _context.StoreStockBalances.AddAsync(balance, ct);
        }

        return balance;
    }

    public Task<Dictionary<int, StoreStockBalance>> GetBalancesAsync(
        int companyId, int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken ct)
        => _context.StoreStockBalances
            .Where(x => x.CompanyId == companyId && x.WarehouseId == warehouseId && productIds.Contains(x.StoreProductId))
            .ToDictionaryAsync(x => x.StoreProductId, ct);

    public async Task<decimal> GetTotalQuantityAsync(int companyId, int productId, CancellationToken ct)
        => await _context.StoreStockBalances
            .Where(x => x.CompanyId == companyId && x.StoreProductId == productId)
            .SumAsync(x => (decimal?)x.Quantity, ct) ?? 0m;

    public Task<Dictionary<int, decimal>> GetWarehouseQuantitiesAsync(
        int companyId, int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken ct)
        => _context.StoreStockBalances.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.WarehouseId == warehouseId && productIds.Contains(x.StoreProductId))
            .ToDictionaryAsync(x => x.StoreProductId, x => x.Quantity, ct);

    public async Task<List<StoreStockBalance>> SearchBalancesAsync(
        int companyId, int? warehouseId, string? term, bool lowOnly, CancellationToken ct)
    {
        var query = _context.StoreStockBalances.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.StoreProduct).ThenInclude(p => p.Barcodes)
            .Where(x => x.CompanyId == companyId);

        if (warehouseId is { } w)
            query = query.Where(x => x.WarehouseId == w);
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(x => x.StoreProduct.Name.Contains(t)
                || (x.StoreProduct.Sku != null && x.StoreProduct.Sku.Contains(t))
                || x.StoreProduct.Barcodes.Any(b => b.Barcode.Contains(t)));
        }
        if (lowOnly)
            query = query.Where(x => x.Quantity <= x.StoreProduct.MinimumStock);

        return await query.OrderBy(x => x.StoreProduct.Name).ToListAsync(ct);
    }

    public async Task AddMovementAsync(StoreStockMovement movement, CancellationToken ct)
        => await _context.StoreStockMovements.AddAsync(movement, ct);

    public Task<List<StoreStockMovement>> SearchMovementsAsync(
        int companyId, int? warehouseId, int? productId, DateTime? fromUtc, DateTime? toUtcExclusive, int take, CancellationToken ct)
        => _context.StoreStockMovements.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.StoreProduct)
            .Where(x => x.CompanyId == companyId
                && (warehouseId == null || x.WarehouseId == warehouseId)
                && (productId == null || x.StoreProductId == productId)
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive))
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(ct);

    // ---- Tills and shifts -------------------------------------------------------------------

    public Task<List<StoreCashRegister>> GetRegistersAsync(int companyId, int? restaurantId, CancellationToken ct)
        => _context.StoreCashRegisters.AsNoTracking()
            .Include(x => x.Restaurant)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId && (restaurantId == null || x.RestaurantId == restaurantId))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<StoreCashRegister?> GetRegisterAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreCashRegisters
            .Include(x => x.Restaurant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddRegisterAsync(StoreCashRegister register, CancellationToken ct)
        => await _context.StoreCashRegisters.AddAsync(register, ct);

    public Task<bool> WarehouseInBranchAsync(int companyId, int warehouseId, int restaurantId, CancellationToken ct)
        => _context.Warehouses.AnyAsync(
            w => w.Id == warehouseId && w.CompanyId == companyId && w.RestaurantId == restaurantId, ct);

    public Task<bool> WarehouseExistsAsync(int companyId, int warehouseId, CancellationToken ct)
        => _context.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId, ct);

    public Task<bool> BranchExistsAsync(int companyId, int restaurantId, CancellationToken ct)
        => _context.Restaurants.AnyAsync(r => r.Id == restaurantId && r.CompanyId == companyId, ct);

    public Task<StoreCashShift?> GetShiftAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreCashShifts
            .Include(x => x.StoreCashRegister)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<StoreCashShift?> GetOpenShiftForRegisterAsync(int registerId, CancellationToken ct)
        => _context.StoreCashShifts
            .Include(x => x.StoreCashRegister)
            .FirstOrDefaultAsync(x => x.StoreCashRegisterId == registerId && x.Status == StoreShiftStatus.Open, ct);

    public Task<StoreCashShift?> GetOpenShiftForUserAsync(int companyId, int userId, CancellationToken ct)
        => _context.StoreCashShifts
            .Include(x => x.StoreCashRegister)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.CashierUserId == userId && x.Status == StoreShiftStatus.Open, ct);

    public Task<List<StoreCashShift>> GetOpenShiftsAsync(int companyId, IReadOnlyCollection<int> registerIds, CancellationToken ct)
        => _context.StoreCashShifts.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == StoreShiftStatus.Open && registerIds.Contains(x.StoreCashRegisterId))
            .ToListAsync(ct);

    public Task<List<StoreCashShift>> SearchShiftsAsync(
        int companyId, int? registerId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct)
        => _context.StoreCashShifts.AsNoTracking()
            .Include(x => x.StoreCashRegister)
            .Where(x => x.CompanyId == companyId
                && (registerId == null || x.StoreCashRegisterId == registerId)
                && (fromUtc == null || x.OpenedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.OpenedAtUtc < toUtcExclusive))
            .OrderByDescending(x => x.OpenedAtUtc)
            .Take(200)
            .ToListAsync(ct);

    public async Task AddShiftAsync(StoreCashShift shift, CancellationToken ct)
        => await _context.StoreCashShifts.AddAsync(shift, ct);

    public async Task AddCashMovementAsync(StoreCashMovement movement, CancellationToken ct)
        => await _context.StoreCashMovements.AddAsync(movement, ct);

    public Task<List<StoreCashMovement>> GetCashMovementsAsync(int shiftId, CancellationToken ct)
        => _context.StoreCashMovements.AsNoTracking()
            .Where(x => x.StoreCashShiftId == shiftId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    // ---- Sales ------------------------------------------------------------------------------

    public Task<StoreSale?> GetSaleByOperationAsync(int companyId, Guid operationId, CancellationToken ct)
        => _context.StoreSales
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .Include(x => x.StoreCashRegister).ThenInclude(r => r.Restaurant)
            .Include(x => x.StoreCustomer)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.OperationId == operationId, ct);

    public Task<StoreSale?> GetSaleAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreSales
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .Include(x => x.StoreCashRegister).ThenInclude(r => r.Restaurant)
            .Include(x => x.StoreCustomer)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreSale>> GetSalesForShiftAsync(int shiftId, CancellationToken ct)
        => _context.StoreSales.AsNoTracking()
            .Include(x => x.Payments)
            .Where(x => x.StoreCashShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<(List<StoreSale> Items, int Total)> SearchSalesAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? shiftId, StoreSaleStatus? status, string? documentNo,
        int skip, int take, IReadOnlyCollection<int>? branchIds, CancellationToken ct)
    {
        var query = _context.StoreSales.AsNoTracking()
            .Include(x => x.StoreCashRegister)
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive)
                && (shiftId == null || x.StoreCashShiftId == shiftId)
                && (status == null || x.Status == status)
                && (branchIds == null || branchIds.Contains(x.RestaurantId)));

        if (!string.IsNullOrWhiteSpace(documentNo))
        {
            var d = documentNo.Trim();
            query = query.Where(x => x.DocumentNo.Contains(d));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddSaleAsync(StoreSale sale, CancellationToken ct)
        => await _context.StoreSales.AddAsync(sale, ct);

    // ---- Quick slots and held sales ---------------------------------------------------------

    public Task<List<StoreQuickSlot>> GetQuickSlotsAsync(int registerId, CancellationToken ct)
        => _context.StoreQuickSlots
            .Include(x => x.StoreProduct)
            .Where(x => x.StoreCashRegisterId == registerId)
            .OrderBy(x => x.SlotNumber)
            .ToListAsync(ct);

    public async Task AddQuickSlotAsync(StoreQuickSlot slot, CancellationToken ct)
        => await _context.StoreQuickSlots.AddAsync(slot, ct);

    public void RemoveQuickSlot(StoreQuickSlot slot) => _context.StoreQuickSlots.Remove(slot);

    public Task<List<StoreHeldSale>> GetHeldSalesAsync(int companyId, int registerId, CancellationToken ct)
        => _context.StoreHeldSales.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StoreCashRegisterId == registerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<StoreHeldSale?> GetHeldSaleAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreHeldSales.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddHeldSaleAsync(StoreHeldSale held, CancellationToken ct)
        => await _context.StoreHeldSales.AddAsync(held, ct);

    public void RemoveHeldSale(StoreHeldSale held) => _context.StoreHeldSales.Remove(held);

    // ---- Serial / IMEI units ----------------------------------------------------------------

    public Task<List<StoreSerialUnit>> FindSerialUnitsAsync(int companyId, IReadOnlyCollection<string> serialNumbers, CancellationToken ct)
        => _context.StoreSerialUnits
            .Where(x => x.CompanyId == companyId && serialNumbers.Contains(x.SerialNumber))
            .ToListAsync(ct);

    public Task<List<StoreSerialUnit>> GetSerialUnitsForSaleLinesAsync(IReadOnlyCollection<int> saleLineIds, CancellationToken ct)
        => _context.StoreSerialUnits
            .Where(x => x.StoreSaleLineId != null && saleLineIds.Contains(x.StoreSaleLineId.Value))
            .ToListAsync(ct);

    public Task<List<StoreSerialUnit>> SearchSerialUnitsAsync(
        int companyId, string? term, int? productId, int? warehouseId, StoreSerialStatus? status, int take, CancellationToken ct)
    {
        var query = _context.StoreSerialUnits.AsNoTracking()
            .Include(x => x.StoreProduct)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId
                && (productId == null || x.StoreProductId == productId)
                && (warehouseId == null || x.WarehouseId == warehouseId)
                && (status == null || x.Status == status));
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(x => x.SerialNumber.Contains(t));
        }

        return query.OrderBy(x => x.SerialNumber).Take(take).ToListAsync(ct);
    }

    public Task<StoreSerialUnit?> GetSerialUnitWithHistoryAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreSerialUnits.AsNoTracking()
            .Include(x => x.StoreProduct)
            .Include(x => x.Warehouse)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddSerialUnitAsync(StoreSerialUnit unit, CancellationToken ct)
        => await _context.StoreSerialUnits.AddAsync(unit, ct);

    public async Task AddSerialEventAsync(StoreSerialEvent serialEvent, CancellationToken ct)
        => await _context.StoreSerialEvents.AddAsync(serialEvent, ct);

    // ---- Approvals and returns --------------------------------------------------------------

    public async Task AddApprovalLogAsync(StoreApprovalLog log, CancellationToken ct)
        => await _context.StoreApprovalLogs.AddAsync(log, ct);

    public Task<StoreSale?> GetSaleByDocumentNoAsync(int companyId, string documentNo, CancellationToken ct)
        => _context.StoreSales
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .Include(x => x.StoreCashRegister).ThenInclude(r => r.Restaurant)
            .Include(x => x.StoreCustomer)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentNo == documentNo, ct);

    public Task<StoreSaleReturn?> GetReturnByOperationAsync(int companyId, Guid operationId, CancellationToken ct)
        => _context.StoreSaleReturns
            .Include(x => x.Lines)
            .Include(x => x.StoreSale)
            .Include(x => x.StoreCashRegister)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.OperationId == operationId, ct);

    public Task<StoreSaleReturn?> GetReturnAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreSaleReturns
            .Include(x => x.Lines)
            .Include(x => x.StoreSale)
            .Include(x => x.StoreCashRegister)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreSaleReturn>> GetReturnsForSaleAsync(int saleId, CancellationToken ct)
        => _context.StoreSaleReturns
            .Include(x => x.Lines)
            .Where(x => x.StoreSaleId == saleId)
            .ToListAsync(ct);

    public Task<List<StoreSaleReturn>> GetReturnsForShiftAsync(int shiftId, CancellationToken ct)
        => _context.StoreSaleReturns.AsNoTracking()
            .Where(x => x.StoreCashShiftId == shiftId)
            .ToListAsync(ct);

    public Task<List<StoreSaleReturn>> SearchReturnsAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int take, CancellationToken ct)
        => _context.StoreSaleReturns.AsNoTracking()
            .Include(x => x.StoreSale)
            .Include(x => x.StoreCashRegister)
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddReturnAsync(StoreSaleReturn saleReturn, CancellationToken ct)
        => await _context.StoreSaleReturns.AddAsync(saleReturn, ct);

    // ---- Shared -----------------------------------------------------------------------------

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
