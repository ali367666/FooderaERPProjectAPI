using Domain.Enums;

namespace Application.CashMovement.Dtos;

public class CreateCashMovementRequest
{
    public int RestaurantId { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    /// <summary>Name of the expense category (optional).</summary>
    public string? Category { get; set; }
    /// <summary>Address returned by POST /api/CashMovements/attachment (optional).</summary>
    public string? AttachmentUrl { get; set; }
}
