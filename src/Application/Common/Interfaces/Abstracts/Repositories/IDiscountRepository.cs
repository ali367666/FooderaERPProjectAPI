using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IDiscountRepository
{
    Task<List<Discount>> GetAllAsync(int companyId, CancellationToken ct);
    Task<Discount?> GetByIdAsync(int id, int companyId, CancellationToken ct);
    Task<Discount?> GetByCodeAsync(string code, int companyId, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId, CancellationToken ct);
    /// <summary>
    /// Of the given menu items, the ones that take no part in discounts: the item itself is marked, or its category
    /// (or any parent category above it) is.
    /// </summary>
    Task<HashSet<int>> GetDiscountExcludedMenuItemIdsAsync(int companyId, IReadOnlyCollection<int> menuItemIds, CancellationToken ct);

    Task AddAsync(Discount discount, CancellationToken ct);
    void Update(Discount discount);
    void Remove(Discount discount);
    Task SaveChangesAsync(CancellationToken ct);
}
