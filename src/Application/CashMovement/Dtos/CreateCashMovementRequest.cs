using Domain.Enums;

namespace Application.CashMovement.Dtos;

public class CreateCashMovementRequest
{
    public int RestaurantId { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
