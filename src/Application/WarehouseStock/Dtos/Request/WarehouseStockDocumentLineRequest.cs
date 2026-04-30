namespace Application.WarehouseStock.Dtos.Request;

public class WarehouseStockDocumentLineRequest
{
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
}
