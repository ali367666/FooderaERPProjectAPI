namespace Application.Reports.Dtos;

public class ReportStockBalanceLineDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public decimal Quantity { get; set; }
}
