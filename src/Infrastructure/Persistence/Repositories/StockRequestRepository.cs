using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
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

    public async Task AddAsync(Domain.Entities.StockRequest stockRequest, CancellationToken cancellationToken)
    {
        await _context.StockRequests.AddAsync(stockRequest, cancellationToken);
    }

    public async Task<Domain.Entities.StockRequest?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<  Domain.Entities.StockRequest?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Domain.Entities.StockRequest>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List< Domain.Entities.StockRequest>> GetByStatusAsync(int companyId, StockRequestStatus status, CancellationToken cancellationToken)
    {
        return await _context.StockRequests
            .Where(x => x.CompanyId == companyId && x.Status == status)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Update(Domain.Entities.StockRequest stockRequest)
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
}