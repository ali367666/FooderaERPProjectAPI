namespace Application.WarehouseStock.Dtos.Response;

public class WarehouseStockBalanceResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
}
