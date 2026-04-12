using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Table)
            .Include(x => x.Waiter)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Order>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Table)
            .Include(x => x.Waiter)
            .Include(x => x.Lines)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenOrderForTableAsync(int tableId, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders.AnyAsync(x =>
            x.TableId == tableId &&
            x.CompanyId == companyId &&
            x.Status != Domain.Enums.OrderStatus.Paid &&
            x.Status != Domain.Enums.OrderStatus.Cancelled,
            cancellationToken);
    }

    public async Task<bool> ExistsByOrderNumberAsync(string orderNumber, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Orders.AnyAsync(x =>
            x.OrderNumber == orderNumber &&
            x.CompanyId == companyId,
            cancellationToken);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    public void Delete(Order order)
    {
        _context.Orders.Remove(order);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}