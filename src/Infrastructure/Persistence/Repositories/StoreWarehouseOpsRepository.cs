using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreWarehouseOpsRepository : IStoreWarehouseOpsRepository
{
    private readonly AppDbContext _context;

    public StoreWarehouseOpsRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<StoreInventory?> GetInventoryAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreInventories
            .Include(x => x.Warehouse)
            .Include(x => x.Category)
            .Include(x => x.Lines).ThenInclude(l => l.StoreProduct)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreInventory>> SearchInventoriesAsync(int companyId, int? warehouseId, StoreInventoryStatus? status, CancellationToken ct)
        => _context.StoreInventories.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Category)
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId
                && (warehouseId == null || x.WarehouseId == warehouseId)
                && (status == null || x.Status == status))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);

    public async Task AddInventoryAsync(StoreInventory inventory, CancellationToken ct)
        => await _context.StoreInventories.AddAsync(inventory, ct);

    public Task<bool> OpenInventoryExistsAsync(int companyId, int warehouseId, int? categoryId, CancellationToken ct)
        => _context.StoreInventories.AnyAsync(
            x => x.CompanyId == companyId && x.WarehouseId == warehouseId && x.Status == StoreInventoryStatus.Draft
                && (x.CategoryId == null || categoryId == null || x.CategoryId == categoryId), ct);

    public async Task<List<(StoreProduct Product, decimal Quantity)>> GetCountableProductsAsync(
        int companyId, int warehouseId, int? categoryId, CancellationToken ct)
    {
        var rows = await _context.StoreStockBalances.AsNoTracking()
            .Include(b => b.StoreProduct)
            .Where(b => b.CompanyId == companyId && b.WarehouseId == warehouseId
                && b.StoreProduct.IsActive && !b.StoreProduct.TracksSerial
                && (categoryId == null || b.StoreProduct.CategoryId == categoryId))
            .OrderBy(b => b.StoreProduct.Name)
            .ToListAsync(ct);
        return rows.Select(b => (b.StoreProduct, b.Quantity)).ToList();
    }

    public Task<StoreTransfer?> GetTransferAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreTransfers
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.StoreProduct)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<List<StoreTransfer>> SearchTransfersAsync(
        int companyId, StoreTransferStatus? status, int? warehouseId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct)
        => _context.StoreTransfers.AsNoTracking()
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId
                && (status == null || x.Status == status)
                && (warehouseId == null || x.FromWarehouseId == warehouseId || x.ToWarehouseId == warehouseId)
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);

    public async Task AddTransferAsync(StoreTransfer transfer, CancellationToken ct)
        => await _context.StoreTransfers.AddAsync(transfer, ct);

    public void RemoveTransferLine(StoreTransferLine line) => _context.StoreTransferLines.Remove(line);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
