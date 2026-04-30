namespace Application.Common.Models;

public class AuditLogEntry
{
    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ActionType { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string Message { get; set; } = default!;
    public int? UserId { get; set; }
    public int? CompanyId { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsSuccess { get; set; } = true;
}