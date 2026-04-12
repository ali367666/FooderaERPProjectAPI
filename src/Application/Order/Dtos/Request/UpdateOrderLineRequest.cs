namespace Application.Order.Dtos.Request;

public class UpdateOrderLineRequest
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? Status { get; set; }
}