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

public class ReturnableOrderLineResponse
{
    public int OrderLineId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public bool IsWeightBased { get; set; }
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReturnableQuantity { get; set; }
    public decimal LineTotal { get; set; }
    /// <summary>Refund per unit of Quantity, after line and order-level discounts.</summary>
    public decimal RefundUnitAmount { get; set; }
}

public class CreateSaleReturnRequest
{
    public int OrderId { get; set; }
    public string? Reason { get; set; }
    public bool RestockItems { get; set; } = true;
    public List<CreateSaleReturnLineRequest> Lines { get; set; } = new();
}

public class CreateSaleReturnLineRequest
{
    public int OrderLineId { get; set; }
    public int Quantity { get; set; }
}

public class SaleReturnResponse
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = default!;
    public int OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public bool RestockItems { get; set; }
    public string? Reason { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<SaleReturnLineResponse> Lines { get; set; } = new();
}

public class SaleReturnLineResponse
{
    public int OrderLineId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}
