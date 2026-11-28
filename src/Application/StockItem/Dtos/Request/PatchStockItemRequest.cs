using Domain.Enums;

namespace Application.StockItem.Dtos.Request;

public class PatchStockItemRequest
{
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public StockItemType? Type { get; set; }
    public UnitOfMeasure? Unit { get; set; }
    public int? CategoryId { get; set; }
    public decimal? SalePrice { get; set; }
    /// <summary>Optional; empty keeps the saved value.</summary>
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public decimal? MinimumStock { get; set; }
}