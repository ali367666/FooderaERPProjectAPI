namespace Application.WarehouseTransfers.Dtos.Request;

public class UpdateWarehouseTransferLineRequest
{
    public int? Id { get; set; }
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
}