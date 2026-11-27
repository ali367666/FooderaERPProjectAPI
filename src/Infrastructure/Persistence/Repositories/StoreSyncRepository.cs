using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreSyncRepository : IStoreSyncRepository
{
    private readonly AppDbContext _context;

    public StoreSyncRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<StoreSyncOperation?> GetByOperationAsync(int companyId, Guid operationId, CancellationToken ct)
        => _context.StoreSyncOperations.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.OperationId == operationId, ct);

    public async Task AddAsync(StoreSyncOperation operation, CancellationToken ct)
        => await _context.StoreSyncOperations.AddAsync(operation, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);

    public Task<List<StoreSyncOperation>> SearchAsync(int companyId, StoreSyncStatus? status, string? deviceId, int take, CancellationToken ct)
        => _context.StoreSyncOperations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (status == null || x.Status == status) && (deviceId == null || x.DeviceId == deviceId))
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(ct);
}
