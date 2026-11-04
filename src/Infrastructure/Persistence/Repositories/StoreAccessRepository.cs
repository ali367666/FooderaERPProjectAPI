using Application.StoreMode.Access;
using Domain.Entities.StoreMode;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreAccessRepository : IStoreAccessRepository
{
    private readonly AppDbContext _context;

    public StoreAccessRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<int>> GetBranchIdsAsync(int companyId, int userId, CancellationToken ct)
        => _context.StoreUserBranches.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => x.RestaurantId).ToListAsync(ct);

    public async Task ReplaceBranchIdsAsync(int companyId, int userId, IReadOnlyCollection<int> branchIds, CancellationToken ct)
    {
        var current = await _context.StoreUserBranches.Where(x => x.CompanyId == companyId && x.UserId == userId).ToListAsync(ct);
        _context.StoreUserBranches.RemoveRange(current.Where(x => !branchIds.Contains(x.RestaurantId)));
        foreach (var id in branchIds.Where(id => current.All(x => x.RestaurantId != id)))
            _context.StoreUserBranches.Add(new StoreUserBranch { CompanyId = companyId, UserId = userId, RestaurantId = id });
        await _context.SaveChangesAsync(ct);
    }

    public Task<int?> GetWarehouseBranchAsync(int companyId, int warehouseId, CancellationToken ct)
        => _context.Warehouses.AsNoTracking()
            .Where(w => w.Id == warehouseId && w.CompanyId == companyId)
            .Select(w => w.RestaurantId).FirstOrDefaultAsync(ct);

    public Task<List<int>> GetWarehouseIdsInBranchesAsync(int companyId, IReadOnlyCollection<int> branchIds, CancellationToken ct)
        => _context.Warehouses.AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.RestaurantId != null && branchIds.Contains(w.RestaurantId.Value))
            .Select(w => w.Id).ToListAsync(ct);
}
