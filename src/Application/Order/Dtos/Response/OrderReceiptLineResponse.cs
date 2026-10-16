namespace Application.Orders.Dtos;

public class OrderReceiptLineResponse
{
    public string MenuItemName { get; set; } = default!;
    public int MenuCategoryId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatAmount { get; set; }
    /// <summary>Complimentary item — its name already carries the "(Hədiyyə)" mark for printing.</summary>
    public bool IsGift { get; set; }

    /// <summary>
    /// On a part-payment receipt: this item was already paid by an earlier guest. It is listed (so the
    /// receipt tells the whole story) but its amount is not part of this receipt's total.
    /// </summary>
    public bool PaidEarlier { get; set; }
}
