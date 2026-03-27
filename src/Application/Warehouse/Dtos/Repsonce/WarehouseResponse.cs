using Domain.Enums;

namespace Application.Warehouse.Dtos.Response;

public class WarehouseResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public WarehouseType Type { get; set; }

    public int CompanyId { get; set; }

    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }

    public int? DriverUserId { get; set; }
    public string? DriverFullName { get; set; }
}