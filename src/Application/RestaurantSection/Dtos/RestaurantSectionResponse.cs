namespace Application.RestaurantSection.Dtos;

public class RestaurantSectionResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public Domain.Enums.RestaurantTableType Type { get; set; }
}
