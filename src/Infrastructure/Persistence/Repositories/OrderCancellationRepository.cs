using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderCancellationRepository : IOrderCancellationRepository
{
    private readonly AppDbContext _context;

    public OrderCancellationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderCancellation cancellation, CancellationToken cancellationToken)
    {
        if (cancellation.CreatedByUserId is int userId && userId > 0)
        {
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.UserName })
                .FirstOrDefaultAsync(cancellationToken);
            cancellation.CancelledByName = user?.FullName ?? user?.UserName ?? $"#{userId}";
        }
        else
        {
            cancellation.CancelledByName = "Naməlum";
        }

        await _context.OrderCancellations.AddAsync(cancellation, cancellationToken);
    }
}
