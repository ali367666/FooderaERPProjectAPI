namespace Application.Restaurant.Dtos.Request;

public class CreateRestaurantRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int CompanyId { get; set; }
}
