using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class QrMenuRepository : IQrMenuRepository
{
    private readonly AppDbContext _context;

    public QrMenuRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<PublicMenuSnapshot?> GetSnapshotAsync(int restaurantId, CancellationToken cancellationToken)
        => _context.PublicMenuSnapshots.FirstOrDefaultAsync(x => x.RestaurantId == restaurantId, cancellationToken);

    public async Task AddSnapshotAsync(PublicMenuSnapshot snapshot, CancellationToken cancellationToken)
        => await _context.PublicMenuSnapshots.AddAsync(snapshot, cancellationToken);

    public Task<List<RestaurantTable>> GetQrTablesAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
        => _context.RestaurantTables
            .Include(x => x.Section)
            .Where(x => x.CompanyId == companyId
                && x.RestaurantId == restaurantId
                && x.IsActive
                && x.Type != RestaurantTableType.Delivery
                && x.Type != RestaurantTableType.TakeAway)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<RestaurantTable?> FindTableByTokenAsync(int restaurantId, string token, CancellationToken cancellationToken)
        => _context.RestaurantTables
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId && x.IsActive && x.QrToken == token, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
