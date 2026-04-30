namespace Application.WarehouseStock.Dtos.Response;

public class WarehouseStockDocumentDetailResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int CompanyId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int Status { get; set; }
    public List<WarehouseStockDocumentLineResponse> Lines { get; set; } = new();
}

public class WarehouseStockDocumentLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
}
