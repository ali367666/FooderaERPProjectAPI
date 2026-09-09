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

public class PublicMenuCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public List<PublicMenuItemResponse> Items { get; set; } = new();
}

public class PublicMenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public bool IsAvailable { get; set; }
}
