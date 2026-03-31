using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;

namespace Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            ActionType = entry.ActionType,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            Message = entry.Message,
            UserId = entry.UserId,
            CompanyId = entry.CompanyId,
            CorrelationId = entry.CorrelationId,
            IsSuccess = entry.IsSuccess
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}