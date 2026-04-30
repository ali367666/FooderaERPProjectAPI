namespace Application.Notifications.Dtos.Response;

public class NotificationResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public string Type { get; set; } = default!;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
