namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IScaleDeviceRepository
{
    Task AddAsync(Domain.Entities.ScaleDevice device, CancellationToken cancellationToken);
    Task<Domain.Entities.ScaleDevice?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.ScaleDevice>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.ScaleDevice device);
    void Delete(Domain.Entities.ScaleDevice device);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
