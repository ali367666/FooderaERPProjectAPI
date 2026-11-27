using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    /// <summary>All company orders, newest first; when <paramref name="filter"/> is given, only matching orders.</summary>
    /// <summary>
    /// Marks the given order lines as sent to the kitchen at <paramref name="at"/>, but only those not sent yet, in one atomic step;
    /// returns the ids this call actually claimed. A line claimed by an earlier (or a simultaneous) OK is not claimed again.
    /// </summary>
    Task<List<int>> ClaimKitchenLinesAsync(IReadOnlyCollection<int> lineIds, DateTime at, CancellationToken cancellationToken);

    /// <summary>Gives the claimed lines back (printing failed), so the next OK sends them.</summary>
    Task ReleaseKitchenLinesAsync(IReadOnlyCollection<int> lineIds, CancellationToken cancellationToken);

    /// <summary>Paid orders of the company (one branch or all) paid inside [fromUtc, toUtcExclusive), newest first, without their lines.</summary>
    Task<List<Application.Orders.Queries.Receipts.ReceiptListItem>> GetReceiptsAsync(int companyId, int? restaurantId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken);

    Task<List<Domain.Entities.Order>> GetAllAsync(int companyId, Application.Orders.Queries.GetAll.OrderListFilter? filter, CancellationToken cancellationToken);
    /// <summary>Every unpaid, uncancelled order on the given tables, newest first, with the waiter and the customer.</summary>
    Task<List<Domain.Entities.Order>> GetActiveForTablesAsync(int companyId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken);

    Task<bool> HasOpenOrderForTableAsync(int tableId, int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber, int companyId, CancellationToken cancellationToken);
    void Update(Domain.Entities.Order order);
    void Delete(Domain.Entities.Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<Domain.Entities.Order?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Order>> GetPaidBetweenAsync(int companyId, int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken);
}