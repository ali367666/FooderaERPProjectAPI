using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PrinterStationTypeRepository : IPrinterStationTypeRepository
{
    private readonly AppDbContext _context;

    public PrinterStationTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PrinterStationType stationType, CancellationToken cancellationToken)
    {
        await _context.PrinterStationTypes.AddAsync(stationType, cancellationToken);
    }

    public async Task<PrinterStationType?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.PrinterStationTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<PrinterStationType>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.PrinterStationTypes
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.PrinterStationTypes
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(PrinterStationType stationType) => _context.PrinterStationTypes.Update(stationType);
    public void Delete(PrinterStationType stationType) => _context.PrinterStationTypes.Remove(stationType);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
