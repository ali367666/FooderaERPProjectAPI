namespace Application.Analytics.Dtos;

public class TopMenuItemDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}
