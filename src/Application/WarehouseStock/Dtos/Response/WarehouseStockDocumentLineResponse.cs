namespace Application.WarehouseStock.Dtos.Response;

public class WarehouseStockDocumentLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
}
