namespace Application.Orders.Dtos;

public class OrderPaymentResponse
{
    public int Id { get; set; }
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public List<OrderPaymentLineResponse> Lines { get; set; } = new();
}
