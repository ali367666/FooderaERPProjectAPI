namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ICounterpartyCategoryRepository
{
    Task AddAsync(Domain.Entities.CounterpartyCategory category, CancellationToken cancellationToken);
    Task<Domain.Entities.CounterpartyCategory?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.CounterpartyCategory>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.CounterpartyCategory category);
    void Delete(Domain.Entities.CounterpartyCategory category);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
