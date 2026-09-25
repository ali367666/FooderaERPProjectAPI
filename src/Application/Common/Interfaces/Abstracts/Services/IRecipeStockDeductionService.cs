using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Services;

public interface IRecipeStockDeductionService
{
    Task DeductForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken);
    Task RestoreForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken);
    Task RestoreForReturnAsync(Domain.Entities.Order order, OrderLine orderLine, int quantity, string returnNumber, CancellationToken cancellationToken);
}
