namespace Application.WarehouseTransfers.Dtos.Response;

public class WarehouseTransferLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
}