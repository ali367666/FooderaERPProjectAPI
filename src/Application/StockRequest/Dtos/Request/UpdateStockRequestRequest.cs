using Application.StockRequest.Dtos.Request;

public class UpdateStockRequestRequest
{
    public int RequestingWarehouseId { get; set; }
    public int SupplyingWarehouseId { get; set; }
    public string? Note { get; set; }
    public List<UpdateStockRequestLineRequest> Lines { get; set; } = new();
}