namespace Application.RestaurantTable.Dtos.Request;

public class CreateRestaurantTableRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
}