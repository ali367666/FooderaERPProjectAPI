namespace Application.WarehouseStock.Dtos.Request;

public class UpdateWarehouseStockDocumentRequest
{
    public int WarehouseId { get; set; }
    public List<WarehouseStockDocumentLineRequest> Lines { get; set; } = new();
}
