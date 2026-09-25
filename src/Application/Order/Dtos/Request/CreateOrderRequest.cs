namespace Application.Orders.Dtos;

public class CreateOrderRequest
{
    public int RestaurantId { get; set; }

    /// <summary>Required unless <see cref="IsDelivery"/> is true, in which case the handler
    /// auto-creates a dedicated virtual table for this one delivery order.</summary>
    public int? TableId { get; set; }

    public int WaiterId { get; set; }
    public string? Note { get; set; }
    public int? GuestCount { get; set; }

    public bool IsDelivery { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
}