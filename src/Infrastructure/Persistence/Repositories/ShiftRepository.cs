using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly AppDbContext _context;

    public ShiftRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Shift shift, CancellationToken cancellationToken)
    {
        await _context.Shifts.AddAsync(shift, cancellationToken);
    }

    public async Task<Shift?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Shifts
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<Shift?> GetOpenShiftAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.Shifts
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId && x.IsOpen)
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Update(Shift shift) => _context.Shifts.Update(shift);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
