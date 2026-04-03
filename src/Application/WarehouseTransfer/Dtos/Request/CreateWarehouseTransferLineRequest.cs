namespace Application.WarehouseTransfer.Dtos.Request;

public class CreateWarehouseTransferLineRequest
{
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
}
