namespace Application.WarehouseStock.Dtos.Request;

public class CreateWarehouseStockRequest
{
    public int StockItemId { get; set; }
    public int WarehouseId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal? MinLevel { get; set; }
}