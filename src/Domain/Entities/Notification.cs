using Domain.Common;

namespace Domain.Entities;

public class Notification : CompanyEntity<int>
{
    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;

    public bool IsRead { get; set; } = false;

    public string Type { get; set; } = default!;
    // Məsələn:
    // StockRequest
    // WarehouseTransfer
    // Approval
    // Info

    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}