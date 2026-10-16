namespace Application.SaleReturn;

public class ReturnableOrderResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public int RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }
    public string? CounterpartyName { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReturnedAmount { get; set; }
    public List<ReturnableOrderLineResponse> Lines { get; set; } = new();
    public List<SaleReturnResponse> Returns { get; set; } = new();
}
