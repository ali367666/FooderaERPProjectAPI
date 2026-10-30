using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StockWriteOffRepository : IStockWriteOffRepository
{
    private readonly AppDbContext _context;

    public StockWriteOffRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockWriteOff writeOff, CancellationToken cancellationToken)
        => await _context.StockWriteOffs.AddAsync(writeOff, cancellationToken);

    public Task<List<StockWriteOff>> SearchAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? warehouseId, CancellationToken cancellationToken)
        => _context.StockWriteOffs
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.StockItem)
            .Include(x => x.StockPurchase)
            .Where(x => x.CompanyId == companyId
                && (fromUtc == null || x.CreatedAtUtc >= fromUtc)
                && (toUtcExclusive == null || x.CreatedAtUtc < toUtcExclusive)
                && (warehouseId == null || x.WarehouseId == warehouseId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
