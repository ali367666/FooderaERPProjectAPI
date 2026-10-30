using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IQrMenuRepository
{
    /// <summary>The branch's published QR menu (tracked), or null when it was never published.</summary>
    Task<PublicMenuSnapshot?> GetSnapshotAsync(int restaurantId, CancellationToken cancellationToken);

    Task AddSnapshotAsync(PublicMenuSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>The branch's active dine-in tables (tracked, with their hall), by name - the ones that get a QR code.</summary>
    Task<List<Domain.Entities.RestaurantTable>> GetQrTablesAsync(int companyId, int restaurantId, CancellationToken cancellationToken);

    /// <summary>The active table of the branch that owns this QR token, or null.</summary>
    Task<Domain.Entities.RestaurantTable?> FindTableByTokenAsync(int restaurantId, string token, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
