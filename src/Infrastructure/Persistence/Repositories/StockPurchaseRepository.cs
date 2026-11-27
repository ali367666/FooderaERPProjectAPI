using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StockPurchaseRepository : IStockPurchaseRepository
{
    private readonly AppDbContext _context;

    public StockPurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockPurchase>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.StockPurchases
            .Include(p => p.Warehouse)
            .Include(p => p.Counterparty)
            .Include(p => p.Payments)
            .Include(p => p.Returns).ThenInclude(r => r.Lines).ThenInclude(l => l.StockItem)
            .Include(p => p.Lines)
                .ThenInclude(l => l.StockItem)
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<StockPurchase?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.StockPurchases
            .Include(p => p.Warehouse)
            .Include(p => p.Counterparty)
            .Include(p => p.Payments)
            .Include(p => p.Returns).ThenInclude(r => r.Lines).ThenInclude(l => l.StockItem)
            .Include(p => p.Lines)
                .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(StockPurchase purchase, CancellationToken cancellationToken = default)
        => await _context.StockPurchases.AddAsync(purchase, cancellationToken);

    public void Update(StockPurchase purchase)
        => _context.StockPurchases.Update(purchase);

    public void Remove(StockPurchase purchase)
        => _context.StockPurchases.Remove(purchase);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public Task<bool> WarehouseExistsAsync(int companyId, int warehouseId, CancellationToken cancellationToken = default)
        => _context.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId, cancellationToken);

    public Task<bool> CounterpartyExistsAsync(int companyId, int counterpartyId, CancellationToken cancellationToken = default)
        => _context.Counterparties.AnyAsync(
            c => c.Id == counterpartyId && c.CompanyId == companyId && !c.IsBank, cancellationToken);

    public Task<int> CountStockItemsAsync(int companyId, IReadOnlyCollection<int> stockItemIds, CancellationToken cancellationToken = default)
        => _context.StockItems.CountAsync(i => i.CompanyId == companyId && stockItemIds.Contains(i.Id), cancellationToken);

    public Task<List<StockItem>> GetStockItemsAsync(int companyId, IReadOnlyCollection<int> stockItemIds, CancellationToken cancellationToken = default)
        => _context.StockItems
            .Where(i => i.CompanyId == companyId && stockItemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    public async Task<decimal> GetTotalStockQuantityAsync(int companyId, int stockItemId, CancellationToken cancellationToken = default)
        => await _context.WarehouseStocks
            .Where(s => s.CompanyId == companyId && s.StockItemId == stockItemId)
            .SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0m;
}
