using Domain.Enums;

namespace Application.StockItem.Dtos.Request;

public class GetAllStockItemsRequest
{
    public int? CompanyId { get; set; }
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public StockItemType? Type { get; set; }
}