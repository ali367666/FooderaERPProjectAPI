namespace Application.StockRequests.Dtos.Request;

public class CreateStockRequestRequest
{
    public int CompanyId { get; set; }
    public int RequestingWarehouseId { get; set; }
    public int SupplyingWarehouseId { get; set; }
    public string? Note { get; set; }
    public List<CreateStockRequestLineRequest> Lines { get; set; } = new();
}