namespace Application.WarehouseStock.Dtos.Response;

public class WarehouseStockDocumentSummaryResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int CompanyId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int Status { get; set; }
    public int LineCount { get; set; }
}
