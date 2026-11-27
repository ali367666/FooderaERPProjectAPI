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

    /// <summary>The top-level category ("Kateqoriya"): the product's own category, or its parent when that one is a sub-category.</summary>
    public string CategoryName { get; set; } = default!;

    /// <summary>The sub-category ("Alt kateqoriya") when the product sits in one, otherwise null.</summary>
    public string? SubCategoryName { get; set; }

    /// <summary>Null when the product's barcode is hidden (HideBarcode).</summary>
    public string? Barcode { get; set; }

    /// <summary>The product's scale code (the PLU column); null when the product's codes are hidden.</summary>
    public string? WeightCode { get; set; }
}
