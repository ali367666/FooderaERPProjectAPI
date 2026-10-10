using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderPaymentRepository : IOrderPaymentRepository
{
    private readonly AppDbContext _context;

    public OrderPaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderPayment payment, CancellationToken cancellationToken)
    {
        await _context.OrderPayments.AddAsync(payment, cancellationToken);
    }

    public async Task<List<OrderPayment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken)
    {
        return await _context.OrderPayments
            .Include(x => x.Lines)
                .ThenInclude(l => l.OrderLine)
                    .ThenInclude(l => l.MenuItem)
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderPayment?> GetByIdAsync(int paymentId, int orderId, CancellationToken cancellationToken)
    {
        return await _context.OrderPayments
            .Include(x => x.Lines)
                .ThenInclude(l => l.OrderLine)
                    .ThenInclude(l => l.MenuItem)
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.OrderId == orderId, cancellationToken);
    }

    public async Task<bool> AnyForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        return await _context.OrderPayments.AnyAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task<bool> AnyByAmountAsync(int orderId, CancellationToken cancellationToken)
    {
        return await _context.OrderPayments
            .AnyAsync(x => x.OrderId == orderId && !x.Lines.Any(), cancellationToken);
    }

    public async Task<int> GetPaidQuantityAsync(int orderLineId, CancellationToken cancellationToken)
    {
        return await _context.OrderPaymentLines
            .Where(x => x.OrderLineId == orderLineId)
            .SumAsync(x => (int?)x.Quantity, cancellationToken) ?? 0;
    }
}
