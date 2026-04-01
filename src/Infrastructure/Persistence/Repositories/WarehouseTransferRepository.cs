using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class WarehouseTransferRepository : IWarehouseTransferRepository
{
    private readonly AppDbContext _context;

    public WarehouseTransferRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken)
    {
        await _context.WarehouseTransfers.AddAsync(warehouseTransfer, cancellationToken);
    }

    public async Task<WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<WarehouseTransfer>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WarehouseTransfer>> GetByStatusAsync(int companyId, TransferStatus status, CancellationToken cancellationToken)
    {
        return await _context.WarehouseTransfers
            .Where(x => x.CompanyId == companyId && x.Status == status)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Update(WarehouseTransfer warehouseTransfer)
    {
        _context.WarehouseTransfers.Update(warehouseTransfer);
    }
}