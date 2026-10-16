using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderCancellationRepository
{
    /// <summary>
    /// Stages a cancellation record (it is saved by the caller's SaveChanges, together with the
    /// order change itself). Fills CancelledByName from the user's account.
    /// </summary>
    Task AddAsync(OrderCancellation cancellation, CancellationToken cancellationToken);
}
