using Application.Common.Models;

namespace Application.Common.Interfaces.Abstracts.Services;

public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}