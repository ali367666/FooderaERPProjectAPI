using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync(
        string? entityName,
        string? entityId,
        string? actionType,
        CancellationToken cancellationToken);

    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}