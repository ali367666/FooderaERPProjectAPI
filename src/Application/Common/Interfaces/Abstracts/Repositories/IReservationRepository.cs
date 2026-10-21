using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync(int companyId, DateTime? date, CancellationToken ct);
    Task<Reservation?> GetByIdAsync(int id, int companyId, CancellationToken ct);

    /// <summary>
    /// A pending/confirmed reservation on the table whose time span overlaps [start, start + duration) —
    /// used to stop two parties being booked on one table at the same time.
    /// </summary>
    Task<Reservation?> FindOverlappingAsync(
        int companyId, int tableId, DateTime start, int durationMinutes, int? excludeReservationId, CancellationToken ct);
    Task AddAsync(Reservation reservation, CancellationToken ct);
    void Update(Reservation reservation);
    void Remove(Reservation reservation);
    Task SaveChangesAsync(CancellationToken ct);
}
