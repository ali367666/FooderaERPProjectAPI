using Domain.Common;
using Domain.Enums;
using Domain.Entities.WarehouseAndStock;

namespace Domain.Entities;

public class MenuItem : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public string? Portion { get; set; }

    public bool IsActive { get; set; } = true;

    public PreparationType PreparationType { get; set; } = PreparationType.Kitchen;

    public int MenuCategoryId { get; set; }
    public MenuCategory MenuCategory { get; set; } = default!;

    // Ümumi
    public int ItemTypeId { get; set; }
    public MenuItemType ItemType { get; set; } = default!;
    public int UnitId { get; set; } = (int)UnitOfMeasure.Piece;
    public decimal? VatPercent { get; set; }
    public string? WeightCode { get; set; }
    public string? Barcode { get; set; }

    // Qiymətlər
    public decimal? StationPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? PackagePrice { get; set; }
    public decimal? SpecialPrice1 { get; set; }
    public decimal? SpecialPrice2 { get; set; }
    public decimal? SpecialPrice3 { get; set; }
    public decimal? SpecialPrice4 { get; set; }
    public decimal? SpecialPrice5 { get; set; }

    // Davranış
    public bool HideFromPosSearch { get; set; }
    public bool HideBarcode { get; set; }
    public bool ExcludeFromDiscount { get; set; }
    public bool SkipTaxCalculation { get; set; }
    public bool IsTimeBased { get; set; }
    public bool AllowQuantityPromptOverride { get; set; }
    public int? PrinterId { get; set; }
    public Printer? Printer { get; set; }

    // Cari stok
    public int? StockItemId { get; set; }
    public WarehouseAndStock.StockItem? StockItem { get; set; }

    // SET (bundle)
    public bool IsSet { get; set; }
    public ICollection<MenuItemSetComponent> SetComponents { get; set; } = new List<MenuItemSetComponent>();

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
    public ICollection<MenuItemRecipeLine> RecipeLines { get; set; } = new List<MenuItemRecipeLine>();
}
