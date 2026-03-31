using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseRepository
{
    Task<Domain.Entities.Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<List<Domain.Entities.Warehouse>> GetAllAsync(CancellationToken cancellationToken);

    Task<List<Domain.Entities.Warehouse>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);

    Task<List<Domain.Entities.Warehouse>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken);

    Task<List<Domain.Entities.Warehouse>> GetByDriverUserIdAsync(int driverUserId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    // 🔴 Create üçün
    Task<bool> ExistsByNameAsync(string name, int companyId, CancellationToken cancellationToken);

    // 🟢 Update üçün (IMPORTANT)
    Task<bool> ExistsByNameAsync(string name, int companyId, int excludeId, CancellationToken cancellationToken);

    Task AddAsync(Domain.Entities.Warehouse warehouse, CancellationToken cancellationToken);

    void Update(Domain.Entities.Warehouse warehouse);

    void Delete(Domain.Entities.Warehouse warehouse);

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task UpdateAsync(Domain.Entities.Warehouse warehouse, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Warehouse>> SearchAsync(int companyId, string? search, CancellationToken cancellationToken);
}