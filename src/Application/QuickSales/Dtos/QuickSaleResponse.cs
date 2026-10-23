using Application.Orders.Dtos;

namespace Application.QuickSales.Dtos;

public class QuickSaleResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    /// <summary>The paid customer receipt, so the screen can print it without another request.</summary>
    public OrderReceiptResponse Receipt { get; set; } = default!;
}
