namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IFiscalDeviceRepository
{
    Task AddAsync(Domain.Entities.FiscalDevice device, CancellationToken cancellationToken);
    Task<Domain.Entities.FiscalDevice?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.FiscalDevice>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.FiscalDevice device);
    void Delete(Domain.Entities.FiscalDevice device);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
