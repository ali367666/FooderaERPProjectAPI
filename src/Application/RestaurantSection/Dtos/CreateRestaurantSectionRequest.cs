namespace Application.RestaurantSection.Dtos;

public class CreateRestaurantSectionRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public Domain.Enums.RestaurantTableType Type { get; set; } = Domain.Enums.RestaurantTableType.Masa;
}

public class UpdateRestaurantSectionRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public Domain.Enums.RestaurantTableType Type { get; set; } = Domain.Enums.RestaurantTableType.Masa;
}
