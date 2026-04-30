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

    public async Task<List<Notification>> GetAllForUserAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(x => x.UserId == userId)
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

    public async Task<Notification?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
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

    public async Task<int> GetUnreadCountAllCompaniesAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public async Task<bool> DeleteAsync(
        int id,
        int userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId && x.CompanyId == companyId,
                cancellationToken);

        if (entity == null)
            return false;

        _context.Notifications.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteForUserAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var entity = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entity == null)
            return false;

        _context.Notifications.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}