namespace Application.Order.Dtos.Request;

public class UpdateOrderLineRequest
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? Status { get; set; }
    public decimal? UnitPrice { get; set; }
    public bool? IsGift { get; set; }
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Switch the line to one of the item's own price lists. Any waiter may do this; typing an arbitrary
    /// price (<see cref="UnitPrice"/>) still needs Pos.ChangePrice.
    /// </summary>
    public Domain.Enums.MenuItemPriceType? PriceType { get; set; }
}