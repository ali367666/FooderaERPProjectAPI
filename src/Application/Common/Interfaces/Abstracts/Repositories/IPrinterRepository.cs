namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IPrinterRepository
{
    Task AddAsync(Domain.Entities.Printer printer, CancellationToken cancellationToken);
    Task<Domain.Entities.Printer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Printer>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByStationTypeIdAsync(int stationTypeId, CancellationToken cancellationToken);
    Task<Domain.Entities.Printer?> GetPrimaryAsync(int companyId, int restaurantId, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Printer printer);
    void Delete(Domain.Entities.Printer printer);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
