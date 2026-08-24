using Domain.Entities;

public interface ICompanySettingsRepository
{
    Task<CompanySettings?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task AddAsync(CompanySettings settings, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
