using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStoreSyncRepository
{
    Task<StoreSyncOperation?> GetByOperationAsync(int companyId, Guid operationId, CancellationToken ct);
    Task AddAsync(StoreSyncOperation operation, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<List<StoreSyncOperation>> SearchAsync(int companyId, StoreSyncStatus? status, string? deviceId, int take, CancellationToken ct);
}
