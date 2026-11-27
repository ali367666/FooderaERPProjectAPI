using Domain.Enums;

namespace Application.StockItem.Dtos.Response;

public class StockItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public StockItemType Type { get; set; }
    public UnitOfMeasure Unit { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;

    public int CompanyId { get; set; }
    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public decimal? SalePrice { get; set; }
    /// <summary>Unit price (AZN) of the latest approved purchase.</summary>
    public decimal? LastPurchasePriceAzn { get; set; }
    /// <summary>Weighted-average unit cost (AZN) over the approved purchases.</summary>
    public decimal? AverageCostAzn { get; set; }
}