using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>Product category of the store catalog; categories nest through <see cref="ParentCategoryId"/>.</summary>
public class StoreCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public int? ParentCategoryId { get; set; }
    public StoreCategory? ParentCategory { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>False: products of this category (and of its sub-categories) never earn bonus.</summary>
    public bool EarnsBonus { get; set; } = true;
}

/// <summary>
/// A product the store sells. Its stock per warehouse lives in <see cref="StoreStockBalance"/> and every
/// change of it in <see cref="StoreStockMovement"/>. Prices include VAT.
/// </summary>
public class StoreProduct : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }

    public int? CategoryId { get; set; }
    public StoreCategory? Category { get; set; }
    public string? Brand { get; set; }

    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Piece;

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? WholesalePrice { get; set; }

    /// <summary>Promotion price; applies between the two dates (either may be open).</summary>
    public decimal? CampaignPrice { get; set; }
    public DateTime? CampaignFromUtc { get; set; }
    public DateTime? CampaignToUtc { get; set; }

    /// <summary>What the till charges now: the campaign price while it runs, otherwise the sale price.</summary>
    public decimal EffectivePrice(DateTime nowUtc)
        => CampaignPrice is { } c && (CampaignFromUtc is null || CampaignFromUtc <= nowUtc) && (CampaignToUtc is null || CampaignToUtc >= nowUtc)
            ? c
            : SalePrice;

    /// <summary>VAT included in the sale price, in percent; null or 0 means none.</summary>
    public decimal? VatPercent { get; set; }

    /// <summary>Weighted average cost per unit (AZN); what a sale snapshots as its cost.</summary>
    public decimal AverageCost { get; set; }

    /// <summary>Stock below this raises a low-stock warning.</summary>
    public decimal MinimumStock { get; set; }

    /// <summary>True when every unit carries its own IMEI / serial number.</summary>
    public bool TracksSerial { get; set; }

    /// <summary>False keeps this product out of the base on which bonus is earned.</summary>
    public bool EarnsBonus { get; set; } = true;

    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StoreProductBarcode> Barcodes { get; set; } = new List<StoreProductBarcode>();
}

/// <summary>One scan code of a product; a product may have several. A code is unique within the company.</summary>
public class StoreProductBarcode : CompanyEntity<int>
{
    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public string Barcode { get; set; } = default!;
    public bool IsPrimary { get; set; }
}

/// <summary>Current quantity of a product in a warehouse; kept in step with the movement ledger.</summary>
public class StoreStockBalance : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>Concurrency token: two sales taking the same stock at once cannot both succeed.</summary>
    public byte[] RowVersion { get; set; } = default!;
}

/// <summary>
/// The stock ledger. <see cref="Quantity"/> is signed (plus raises stock, minus lowers it) and the
/// balance before and after is kept, so the stock can be re-derived and audited.
/// </summary>
public class StoreStockMovement : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public StoreStockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    /// <summary>Cost per unit (AZN) at the time of the movement.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>The document behind it ("Sale", "Adjustment"...), its id and number.</summary>
    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }

    public string? Reason { get; set; }
}

/// <summary>Company-level store rules.</summary>
public class StoreSettings : CompanyEntity<int>
{
    /// <summary>When false (the default) a sale that would take stock below zero is refused.</summary>
    public bool AllowNegativeStock { get; set; }

    /// <summary>A sale discount above this percent of the sale needs a manager's approval.</summary>
    public decimal MaxDiscountPercent { get; set; } = 10m;

    /// <summary>Bonus earned as a percent of what the customer really paid; 0 switches bonus earning off.</summary>
    public decimal BonusEarnPercent { get; set; }

    /// <summary>The most of a sale that may be paid with bonus, in percent.</summary>
    public decimal BonusMaxPaymentPercent { get; set; } = 50m;

    /// <summary>Bonus unused for this many days expires; 0 means it never does.</summary>
    public int BonusExpiryDays { get; set; }

    /// <summary>True when sales must be registered with a fiscal device (through the adapter).</summary>
    public bool FiscalEnabled { get; set; }

    /// <summary>Receipt paper width in millimetres (58 or 80).</summary>
    public int ReceiptWidthMm { get; set; } = 80;

    /// <summary>Lines printed at the top / bottom of every receipt.</summary>
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }

    /// <summary>When true a cashier with the "no receipt" permission may complete a sale without printing one.</summary>
    public bool AllowReceiptlessSale { get; set; }

    /// <summary>How many quick-sale buttons the till shows (the admin decides, as many as needed; shown 50 to a page).</summary>
    public int QuickSlotCount { get; set; } = 50;
}
