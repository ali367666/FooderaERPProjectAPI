using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync(
        string? entityName,
        string? entityId,
        string? actionType,
        int? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? search,
        CancellationToken cancellationToken);

    Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}