namespace Application.Pos.Dtos;

/// <summary>An open order the POS search found (table, take-away or delivery).</summary>
public class PosSearchOrderResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public string Status { get; set; } = default!;
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public Domain.Enums.RestaurantTableType? TableType { get; set; }
    public bool IsDelivery { get; set; }
    public int WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Note { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public int? GuestCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OpenedAt { get; set; }
}
