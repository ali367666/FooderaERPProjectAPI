namespace Application.Orders.Dtos;

public class PartPaymentResponse
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public bool OrderClosed { get; set; }
    public decimal RemainingAmount { get; set; }
}
