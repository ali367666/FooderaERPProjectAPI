namespace Application.RestaurantTable.Dtos.Request;

public class UpdateRestaurantTableRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
}