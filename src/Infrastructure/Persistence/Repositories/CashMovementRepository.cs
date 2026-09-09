using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CashMovementRepository : ICashMovementRepository
{
    private readonly AppDbContext _context;

    public CashMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CashMovement movement, CancellationToken cancellationToken)
    {
        await _context.CashMovements.AddAsync(movement, cancellationToken);
    }

    public async Task<List<CashMovement>> GetAllByRestaurantAsync(
        int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return await _context.CashMovements
            .Include(x => x.CreatedByUser)
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId
                && x.CreatedAtUtc >= from && x.CreatedAtUtc < to)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
