using Domain.Enums;

namespace Application.CashMovement.Dtos;

public class CashMovementResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateCashMovementRequest
{
    public int RestaurantId { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
