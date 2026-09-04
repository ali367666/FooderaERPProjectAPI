namespace Application.RestaurantTables.Dtos;

public class RestaurantTableResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public bool IsOccupied { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Shape { get; set; } = "square";
    public int Rotation { get; set; }
    public int? SectionId { get; set; }
    public decimal? HourlyRate { get; set; }
    public Domain.Enums.RestaurantTableType Type { get; set; }
}