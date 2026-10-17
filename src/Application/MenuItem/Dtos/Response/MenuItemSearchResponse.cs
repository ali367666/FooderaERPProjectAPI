using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class MenuItemSearchResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    /// <summary>The price the POS will charge for 1 unit (station / package / sale price rules applied).</summary>
    public decimal Price { get; set; }
    public int UnitId { get; set; }
    public bool IsSet { get; set; }
    public bool IsTimeBased { get; set; }
    public PreparationType PreparationType { get; set; }
    public int MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = default!;
}
