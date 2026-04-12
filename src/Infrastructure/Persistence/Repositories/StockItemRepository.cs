using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StockItem.Dtos.Request;
using Domain.Entities.WarehouseAndStock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly AppDbContext _context;

    public StockItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<StockItem>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.StockItems
            .Include(x => x.Category)
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockItem>> GetAllAsync(GetAllStockItemsRequest request, CancellationToken cancellationToken)
    {
        var query = _context.StockItems
            .Include(x => x.Category)
            .AsQueryable();

        if (request.CompanyId.HasValue)
            query = query.Where(x => x.CompanyId == request.CompanyId.Value);

        if (request.Id.HasValue)
            query = query.Where(x => x.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => x.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Barcode))
            query = query.Where(x => x.Barcode != null && x.Barcode.Contains(request.Barcode));

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);

        if (request.Type.HasValue)
            query = query.Where(x => x.Type == request.Type.Value);

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StockItem stockItem, CancellationToken cancellationToken)
    {
        await _context.StockItems.AddAsync(stockItem, cancellationToken);
    }

    public void Update(StockItem stockItem)
    {
        _context.StockItems.Update(stockItem);
    }

    public void Delete(StockItem stockItem)
    {
        _context.StockItems.Remove(stockItem);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockItems.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> BarcodeExistsAsync(string barcode, int companyId, CancellationToken cancellationToken)
    {
        return await _context.StockItems.AnyAsync(
            x => x.Barcode == barcode && x.CompanyId == companyId,
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}