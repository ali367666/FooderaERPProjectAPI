namespace Application.RestaurantTable.Dtos.Request;

public class CreateRestaurantTableRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public decimal? HourlyRate { get; set; }
    public Domain.Enums.RestaurantTableType Type { get; set; } = Domain.Enums.RestaurantTableType.Masa;
}