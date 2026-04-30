using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetAllAsync(
        string? entityName,
        string? entityId,
        string? actionType,
        int? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName.Contains(entityName));

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(x => x.EntityId.Contains(entityId));

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(x => x.ActionType.Contains(actionType));

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                (x.Message != null && x.Message.Contains(s)) ||
                (x.EntityName != null && x.EntityName.Contains(s)) ||
                (x.EntityId != null && x.EntityId.Contains(s)) ||
                (x.ActionType != null && x.ActionType.Contains(s)) ||
                (x.OldValues != null && x.OldValues.Contains(s)) ||
                (x.NewValues != null && x.NewValues.Contains(s)) ||
                (x.User != null && x.User.FullName != null && x.User.FullName.Contains(s)) ||
                (x.User != null && x.User.Email != null && x.User.Email.Contains(s)));
        }

        return await query
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}