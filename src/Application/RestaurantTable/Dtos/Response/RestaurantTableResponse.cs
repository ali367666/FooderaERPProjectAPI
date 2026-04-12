namespace Application.RestaurantTables.Dtos;

public class RestaurantTableResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public bool IsOccupied { get; set; }
}