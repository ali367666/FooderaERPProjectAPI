using Domain.Entities.BscInvoice;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IBscInvoiceRepository
{
    Task<List<BscInvoiceM>> GetAllAsync(DateTime? docDate = null, CancellationToken cancellationToken = default);
    Task<BscInvoiceM?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<BscInvoiceM> invoices, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
