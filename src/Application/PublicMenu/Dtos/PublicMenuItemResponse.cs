namespace Application.PublicMenu.Dtos;

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
