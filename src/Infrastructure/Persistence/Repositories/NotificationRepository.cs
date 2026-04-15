using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserIdAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(x => x.UserId == userId && x.CompanyId == companyId)
            .OrderByDescending(x => x.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(
        int id,
        int userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.CompanyId == companyId,
                cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        int userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(x =>
                x.UserId == userId &&
                x.CompanyId == companyId &&
                !x.IsRead,
                cancellationToken);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}