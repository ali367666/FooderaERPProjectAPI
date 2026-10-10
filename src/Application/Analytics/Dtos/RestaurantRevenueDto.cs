namespace Application.Analytics.Dtos;

public class RestaurantRevenueDto
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
