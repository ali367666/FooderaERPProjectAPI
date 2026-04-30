using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WarehouseStockDocumentRepository : IWarehouseStockDocumentRepository
{
    private readonly AppDbContext _context;

    public WarehouseStockDocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WarehouseStockDocument document, CancellationToken cancellationToken)
    {
        await _context.WarehouseStockDocuments.AddAsync(document, cancellationToken);
    }

    public async Task<WarehouseStockDocument?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseStockDocuments
            .Include(x => x.Lines)
            .ThenInclude(l => l.StockItem)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<WarehouseStockDocument>> SearchByCompanyAsync(
        int companyId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.WarehouseStockDocuments
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.DocumentNo.ToLower().Contains(s) ||
                x.Warehouse.Name.ToLower().Contains(s) ||
                x.Id.ToString().Contains(s));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WarehouseStockDocument>> GetByWarehouseIdAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        return await _context.WarehouseStockDocuments
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Lines)
            .ThenInclude(l => l.StockItem)
            .Where(x => x.WarehouseId == warehouseId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Update(WarehouseStockDocument document)
    {
        _context.WarehouseStockDocuments.Update(document);
    }

    public void Remove(WarehouseStockDocument document)
    {
        _context.WarehouseStockDocuments.Remove(document);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
