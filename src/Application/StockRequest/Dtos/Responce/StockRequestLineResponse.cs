namespace Application.StockRequests.Dtos.Response;

public class StockRequestLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
}