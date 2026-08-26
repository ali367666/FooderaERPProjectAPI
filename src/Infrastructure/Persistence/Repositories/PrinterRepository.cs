using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PrinterRepository : IPrinterRepository
{
    private readonly AppDbContext _context;

    public PrinterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Printer printer, CancellationToken cancellationToken)
    {
        await _context.Printers.AddAsync(printer, cancellationToken);
    }

    public async Task<Printer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Printers
            .Include(x => x.StationType)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Printer>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.Printers
            .Include(x => x.StationType)
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.Printers
            .AnyAsync(x => x.RestaurantId == restaurantId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public async Task<bool> ExistsByStationTypeIdAsync(int stationTypeId, CancellationToken cancellationToken)
    {
        return await _context.Printers.AnyAsync(x => x.StationTypeId == stationTypeId, cancellationToken);
    }

    public void Update(Printer printer) => _context.Printers.Update(printer);
    public void Delete(Printer printer) => _context.Printers.Remove(printer);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
