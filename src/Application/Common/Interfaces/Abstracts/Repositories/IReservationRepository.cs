using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync(int companyId, DateTime? date, CancellationToken ct);
    Task<Reservation?> GetByIdAsync(int id, int companyId, CancellationToken ct);

    /// <summary>
    /// The pending/confirmed reservation that keeps the table closed right now: from
    /// <paramref name="blockMinutes"/> before its time until it ends. Null when the table is free.
    /// </summary>
    Task<Reservation?> FindBlockingForTableAsync(
        int companyId, int tableId, DateTime nowLocal, int blockMinutes, CancellationToken ct);
    Task AddAsync(Reservation reservation, CancellationToken ct);
    void Update(Reservation reservation);
    void Remove(Reservation reservation);
    Task SaveChangesAsync(CancellationToken ct);
}
