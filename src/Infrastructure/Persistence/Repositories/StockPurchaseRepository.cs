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
            .Include(p => p.Lines)
                .ThenInclude(l => l.StockItem)
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<StockPurchase?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.StockPurchases
            .Include(p => p.Warehouse)
            .Include(p => p.Counterparty)
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
}
