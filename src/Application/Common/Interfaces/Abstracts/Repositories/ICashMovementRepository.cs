namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ICashMovementRepository
{
    Task AddAsync(Domain.Entities.CashMovement movement, CancellationToken cancellationToken);
    Task<List<Domain.Entities.CashMovement>> GetAllByRestaurantAsync(
        int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
