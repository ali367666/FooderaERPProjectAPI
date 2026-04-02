namespace Application.StockRequests.Dtos.Request;

public class CreateStockRequestLineRequest
{
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
}