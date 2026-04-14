using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;

namespace Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
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
            UserId = _currentUserService.UserId,
            CompanyId = _currentUserService.CompanyId,
            CorrelationId = entry.CorrelationId,
            IsSuccess = entry.IsSuccess
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}