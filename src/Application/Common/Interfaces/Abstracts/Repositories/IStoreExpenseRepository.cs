using Domain.Entities.StoreMode;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Expense items and expense documents (digər xərclər) of the store.</summary>
public interface IStoreExpenseRepository
{
    Task<List<StoreExpenseItem>> GetItemsAsync(int companyId, bool includeInactive, CancellationToken ct);
    Task<StoreExpenseItem?> GetItemAsync(int id, int companyId, CancellationToken ct);
    Task<bool> ItemNameExistsAsync(int companyId, string name, int? exceptId, CancellationToken ct);
    Task AddItemAsync(StoreExpenseItem item, CancellationToken ct);

    Task<StoreExpense?> GetExpenseAsync(int id, int companyId, CancellationToken ct);

    /// <summary>Expense documents with their lines, till and item, newest first (at most 1000).</summary>
    Task<List<StoreExpense>> SearchAsync(int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct);

    Task AddExpenseAsync(StoreExpense expense, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
