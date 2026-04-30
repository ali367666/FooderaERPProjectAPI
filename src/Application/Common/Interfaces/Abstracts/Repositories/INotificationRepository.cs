using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<List<Notification>> GetByUserIdAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    /// <summary>All notifications for the user (any company). Used when API company filter is not set.</summary>
    Task<List<Notification>> GetAllForUserAsync(int userId, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(
        int id,
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    /// <summary>Lookup by id and user only (company taken from row).</summary>
    Task<Notification?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAllCompaniesAsync(int userId, CancellationToken cancellationToken);

    void Update(Notification notification);

    Task<bool> DeleteAsync(
        int id,
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    Task<bool> DeleteForUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}