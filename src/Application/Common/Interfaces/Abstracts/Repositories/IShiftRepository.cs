namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IShiftRepository
{
    Task AddAsync(Domain.Entities.Shift shift, CancellationToken cancellationToken);
    Task<Domain.Entities.Shift?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<Domain.Entities.Shift?> GetOpenShiftAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Shift shift);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
