namespace Application.Pos.Dtos;

/// <summary>A table the POS search found, with the order currently open on it (if any).</summary>
public class PosSearchTableResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool IsOccupied { get; set; }
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }
    public string? Note { get; set; }
    public Domain.Enums.RestaurantTableType Type { get; set; }

    public int? ActiveOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? OrderStatus { get; set; }
    public string? WaiterName { get; set; }
}
