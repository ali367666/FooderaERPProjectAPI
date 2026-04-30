using Domain.Enums;

namespace Application.Warehouse.Dtos.Request;

public class UpdateWarehouseRequest
{
    public string Name { get; set; } = default!;
    public WarehouseType Type { get; set; }

    public int CompanyId { get; set; }
    public int? RestaurantId { get; set; }
    public int? ResponsibleEmployeeId { get; set; }
    public int? DriverUserId { get; set; }
}