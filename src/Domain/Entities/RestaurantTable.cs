using Domain.Common;

namespace Domain.Entities;

public class RestaurantTable : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsOccupied { get; set; } = false;

    public int? SectionId { get; set; }
    public RestaurantSection? Section { get; set; }

    // Floor plan position
    public int PosX { get; set; } = 0;
    public int PosY { get; set; } = 0;
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public string Shape { get; set; } = "square"; // square, round, rectangle
    public int Rotation { get; set; } = 0;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}