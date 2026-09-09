using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class CashMovement : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }

    /// <summary>Navigation for the inherited BaseEntity.CreatedByUserId — lets the UI show who made the movement.</summary>
    public User? CreatedByUser { get; set; }
}
