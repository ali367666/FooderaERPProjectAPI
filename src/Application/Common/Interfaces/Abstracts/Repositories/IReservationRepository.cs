using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync(int companyId, DateTime? date, CancellationToken ct);
    Task<Reservation?> GetByIdAsync(int id, int companyId, CancellationToken ct);
    Task AddAsync(Reservation reservation, CancellationToken ct);
    void Update(Reservation reservation);
    void Remove(Reservation reservation);
    Task SaveChangesAsync(CancellationToken ct);
}
