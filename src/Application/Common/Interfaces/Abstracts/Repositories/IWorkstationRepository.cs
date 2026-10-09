namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWorkstationRepository
{
    Task AddAsync(Domain.Entities.Workstation workstation, CancellationToken cancellationToken);
    Task<Domain.Entities.Workstation?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Workstation>> GetAllByCompanyAsync(int companyId, int? restaurantId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Workstation>> GetActiveByCompanyAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken);
    Task TouchLastSeenAsync(int id, int companyId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Workstation workstation);
    void Delete(Domain.Entities.Workstation workstation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
