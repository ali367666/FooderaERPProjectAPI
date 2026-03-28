using Domain.Enums;

namespace Application.StockItem.Dtos.Request;

public class PatchStockItemRequest
{
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public StockItemType? Type { get; set; }
    public UnitOfMeasure? Unit { get; set; }
    public int? CategoryId { get; set; }
}