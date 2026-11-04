using Domain.Entities.StoreMode;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Data access of store customers, their bonus ledger and the debt payments taken at the till.</summary>
public interface IStoreCustomerRepository
{
    Task<List<StoreCustomer>> SearchCustomersAsync(int companyId, string? term, bool includeInactive, int take, CancellationToken ct);
    Task<StoreCustomer?> GetCustomerAsync(int id, int companyId, CancellationToken ct);

    /// <summary>The customer whose code or phone is exactly this, or the only one whose name/phone contains it.</summary>
    Task<StoreCustomer?> FindCustomerAsync(int companyId, string term, CancellationToken ct);
    Task<bool> CodeExistsAsync(int companyId, string code, int? exceptId, CancellationToken ct);
    Task AddCustomerAsync(StoreCustomer customer, CancellationToken ct);

    Task AddBonusEntryAsync(StoreBonusEntry entry, CancellationToken ct);
    Task<decimal> GetBonusBalanceAsync(int companyId, int customerId, CancellationToken ct);
    Task<Dictionary<int, decimal>> GetBonusBalancesAsync(int companyId, CancellationToken ct);
    Task<List<StoreBonusEntry>> GetBonusEntriesAsync(int companyId, int customerId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct);

    Task AddCustomerPaymentAsync(StoreCustomerPayment payment, CancellationToken ct);
    Task<StoreCustomerPayment?> GetCustomerPaymentAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreCustomerPayment>> GetCustomerPaymentsAsync(int companyId, int customerId, CancellationToken ct);
    Task<List<StoreCustomerPayment>> GetPaymentsForShiftAsync(int shiftId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
