using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Services;

public interface IRecipeStockDeductionService
{
    Task DeductForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken);
}
