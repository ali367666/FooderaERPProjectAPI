namespace Application.Orders.Dtos.Request;

public class PayOrderRequest
{
    public string PaymentMethod { get; set; } = default!;
    public decimal PaidAmount { get; set; }
}
