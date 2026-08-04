using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context) => _context = context;

    public async Task<List<Reservation>> GetAllAsync(int companyId, DateTime? date, CancellationToken ct)
    {
        var q = _context.Reservations
            .Include(r => r.Restaurant)
            .Include(r => r.Table)
            .Where(r => r.CompanyId == companyId);

        if (date.HasValue)
            q = q.Where(r => r.ReservationDate.Date == date.Value.Date);

        return await q.OrderBy(r => r.ReservationDate)
                      .ThenBy(r => r.ReservationTime)
                      .ToListAsync(ct);
    }

    public Task<Reservation?> GetByIdAsync(int id, int companyId, CancellationToken ct)
        => _context.Reservations
            .Include(r => r.Restaurant)
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);

    public async Task AddAsync(Reservation reservation, CancellationToken ct)
        => await _context.Reservations.AddAsync(reservation, ct);

    public void Update(Reservation reservation)
        => _context.Reservations.Update(reservation);

    public void Remove(Reservation reservation)
        => _context.Reservations.Remove(reservation);

    public Task SaveChangesAsync(CancellationToken ct)
        => _context.SaveChangesAsync(ct);
}
