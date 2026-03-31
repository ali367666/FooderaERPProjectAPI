using Domain.Common;

public class AuditLog : BaseEntity<long>
{
    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ActionType { get; set; } = default!; // Create, Update, Delete

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public string Message { get; set; } = default!;

    public int? UserId { get; set; }
    public int? CompanyId { get; set; }

    public string? CorrelationId { get; set; }

    public bool IsSuccess { get; set; }
}