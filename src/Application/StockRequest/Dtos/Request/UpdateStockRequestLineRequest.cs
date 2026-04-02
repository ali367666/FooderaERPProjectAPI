namespace Application.StockRequest.Dtos.Request;


public class UpdateStockRequestLineRequest
{
    public int? Id { get; set; }
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
}