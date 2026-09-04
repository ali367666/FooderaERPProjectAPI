using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class MenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public bool IsActive { get; set; }

    public int MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = default!;
    public PreparationType PreparationType { get; set; }

    public int ItemTypeId { get; set; }
    public string ItemTypeName { get; set; } = default!;
    public int UnitId { get; set; }
    public decimal? VatPercent { get; set; }
    public string? WeightCode { get; set; }
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
    public string? PrinterName { get; set; }

    public bool IsSet { get; set; }

    public int? StockItemId { get; set; }
    public string? StockItemName { get; set; }
}
