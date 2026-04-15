using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<List<Notification>> GetByUserIdAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(
        int id,
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken);

    void Update(Notification notification);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}