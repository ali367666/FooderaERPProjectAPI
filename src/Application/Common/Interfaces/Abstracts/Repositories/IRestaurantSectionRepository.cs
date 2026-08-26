namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRestaurantSectionRepository
{
    Task AddAsync(Domain.Entities.RestaurantSection section, CancellationToken cancellationToken);
    Task<Domain.Entities.RestaurantSection?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.RestaurantSection>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.RestaurantSection section);
    void Delete(Domain.Entities.RestaurantSection section);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
