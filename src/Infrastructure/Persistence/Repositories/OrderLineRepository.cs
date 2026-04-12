using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class OrderLineRepository : IOrderLineRepository
{
    private readonly AppDbContext _context;

    public OrderLineRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken)
    {
        await _context.OrderLines.AddAsync(orderLine, cancellationToken);
    }

    public async Task<OrderLine?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.OrderLines
            .Include(x => x.Order)
            .Include(x => x.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<OrderLine>> GetByOrderIdAsync(int orderId, int companyId, CancellationToken cancellationToken)
    {
        return await _context.OrderLines
            .Include(x => x.MenuItem)
            .Where(x => x.OrderId == orderId && x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public void Update(OrderLine orderLine)
    {
        _context.OrderLines.Update(orderLine);
    }

    public void Delete(OrderLine orderLine)
    {
        _context.OrderLines.Remove(orderLine);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}