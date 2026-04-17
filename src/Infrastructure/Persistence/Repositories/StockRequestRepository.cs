using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class StockRequestRepository : IStockRequestRepository
{
    private readonly AppDbContext _context;

    public StockRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockRequest stockRequest, CancellationToken cancellationToken)
    {
        await _context.StockRequests.AddAsync(stockRequest, cancellationToken);
    }

    public async Task<StockRequest?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<StockRequest?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Include(x => x.RequestingWarehouse)
            .Include(x => x.SupplyingWarehouse)
            .Include(x => x.Lines)
                .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<StockRequest>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List< StockRequest>> GetByStatusAsync(int companyId, StockRequestStatus status, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Where(x => x.CompanyId == companyId && x.Status == status)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Update(StockRequest stockRequest)
    {
        _context.StockRequests.Update(stockRequest);
    }

    

    public async Task<List<StockRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Include(x => x.RequestingWarehouse)
            .Include(x => x.SupplyingWarehouse)
            .Include(x => x.Lines)
                .ThenInclude(x => x.StockItem)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(StockRequest stockRequest, CancellationToken cancellationToken)
    {
        _context.StockRequestLines.RemoveRange(stockRequest.Lines);
        _context.StockRequests.Remove(stockRequest);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StockRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Include(x => x.RequestingWarehouse)
            .Include(x => x.SupplyingWarehouse)
            .Include(x => x.Lines)
                .ThenInclude(x => x.StockItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}