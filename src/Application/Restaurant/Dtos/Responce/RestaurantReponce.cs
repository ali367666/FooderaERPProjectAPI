namespace Application.Restaurant.Dtos.Responce;

public class RestaurantResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
}