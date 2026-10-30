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

    /// <summary>The table whose QR code was scanned (null for the branch-wide QR or an unknown token).</summary>
    public string? TableName { get; set; }

    /// <summary>Echo of the scanned table's token - what an order placed from this menu will send back.</summary>
    public string? TableToken { get; set; }

    public List<PublicMenuCategoryResponse> Categories { get; set; } = new();
}
