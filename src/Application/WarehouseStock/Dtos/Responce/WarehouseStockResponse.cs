namespace Application.WarehouseStock.Dtos.Response;

public class WarehouseStockResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public decimal QuantityOnHand { get; set; }
    public decimal? MinLevel { get; set; }
}