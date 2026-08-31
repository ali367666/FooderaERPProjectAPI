namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ICounterpartyRepository
{
    Task AddAsync(Domain.Entities.Counterparty counterparty, CancellationToken cancellationToken);
    Task<Domain.Entities.Counterparty?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Counterparty>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Counterparty counterparty);
    void Delete(Domain.Entities.Counterparty counterparty);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
