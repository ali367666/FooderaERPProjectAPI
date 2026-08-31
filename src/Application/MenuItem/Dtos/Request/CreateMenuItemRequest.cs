using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class CreateMenuItemRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public int MenuCategoryId { get; set; }
    public PreparationType PreparationType { get; set; }

    public int ItemTypeId { get; set; }
    public int UnitId { get; set; } = 1;
    public decimal? VatPercent { get; set; }
    public string? Barcode { get; set; }

    public decimal? StationPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? PackagePrice { get; set; }
    public decimal? SpecialPrice1 { get; set; }
    public decimal? SpecialPrice2 { get; set; }
    public decimal? SpecialPrice3 { get; set; }
    public decimal? SpecialPrice4 { get; set; }
    public decimal? SpecialPrice5 { get; set; }

    public bool HideFromPosSearch { get; set; }
    public bool HideBarcode { get; set; }
    public bool ExcludeFromDiscount { get; set; }
    public bool SkipTaxCalculation { get; set; }
    public bool IsTimeBased { get; set; }
    public bool AllowQuantityPromptOverride { get; set; }
    public int? PrinterId { get; set; }

    public bool IsSet { get; set; }

    public int? StockItemId { get; set; }
}
