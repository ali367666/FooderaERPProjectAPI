using Application.Finance.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IFinanceRepository
{
    Task AddAsync(FinanceEntry entry, CancellationToken cancellationToken);
    Task<FinanceEntry?> GetByIdAsync(int companyId, int id, CancellationToken cancellationToken);
    void Remove(FinanceEntry entry);

    Task<List<FinanceEntry>> GetEntriesAsync(
        int companyId, int? restaurantId, DateTime fromDate, DateTime toDateExclusive,
        FinanceEntryType? type, CancellationToken cancellationToken);

    /// <summary>Sales income, completed stock purchases, till withdrawals and sale returns in the window.</summary>
    Task<List<FinanceSourceLine>> GetAutomaticLinesAsync(
        int companyId, int? restaurantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
