using Domain.Enums;

namespace Application.Warehouse.Dtos.Request;

public class PatchWarehouseRequest
{
    public string? Name { get; set; }
    public WarehouseType? Type { get; set; }

    public int? RestaurantId { get; set; }
    public int? DriverUserId { get; set; }
}
