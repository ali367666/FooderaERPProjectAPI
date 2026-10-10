namespace Application.PublicMenu.Dtos;

public class PublicMenuResponse
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string? Slogan { get; set; }
    public string? ProductColor { get; set; }
    public string? ContactPhoneNumber { get; set; }
    public string? SocialLinks { get; set; }

    public List<PublicMenuCategoryResponse> Categories { get; set; } = new();
}
