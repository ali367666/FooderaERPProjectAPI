namespace Application.Orders.Dtos.Request;

public class PayOrderPartLine
{
    public int OrderLineId { get; set; }

    /// <summary>In the line's own unit — pieces, or grams for weight items.</summary>
    public int Quantity { get; set; }
}
