using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.BscInvoice;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BscInvoiceRepository : IBscInvoiceRepository
{
    private readonly AppDbContext _context;

    public BscInvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BscInvoiceM>> GetAllAsync(DateTime? docDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BscInvoiceMs
            .Include(m => m.Lines)
            .AsQueryable();

        if (docDate.HasValue)
            query = query.Where(m => m.DocDate.Date == docDate.Value.Date);

        return await query.OrderByDescending(m => m.BscInvoiceMId).ToListAsync(cancellationToken);
    }

    public async Task<BscInvoiceM?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.BscInvoiceMs
            .Include(m => m.Lines)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<BscInvoiceM> invoices, CancellationToken cancellationToken = default)
        => await _context.BscInvoiceMs.AddRangeAsync(invoices, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
