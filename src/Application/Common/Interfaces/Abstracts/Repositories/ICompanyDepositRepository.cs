using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ICompanyDepositRepository
{
    Task AddAsync(CompanyDeposit deposit, CancellationToken cancellationToken);

    /// <summary>The company's deposits, oldest first.</summary>
    Task<List<CompanyDeposit>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken);

    Task<bool> HasOpeningAsync(int companyId, CancellationToken cancellationToken);

    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
