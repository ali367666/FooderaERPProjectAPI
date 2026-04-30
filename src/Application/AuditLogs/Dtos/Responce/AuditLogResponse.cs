namespace Application.AuditLogs.Dtos.Response;

public class AuditLogResponse
{
    public long Id { get; set; }
    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ActionType { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string Message { get; set; } = default!;
    public int? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public int? CompanyId { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}