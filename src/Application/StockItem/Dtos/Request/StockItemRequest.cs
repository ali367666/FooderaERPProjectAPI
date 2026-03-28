using Domain.Enums;

namespace Application.StockItem.Dtos.Request;

public class StockItemRequest
{
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public StockItemType Type { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public int CategoryId { get; set; }
    public int CompanyId { get; set; }
    public int? RestaurantId { get; set; }
}