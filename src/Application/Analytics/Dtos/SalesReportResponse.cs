namespace Application.Analytics.Dtos;

public class SalesReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public int OrderCount { get; set; }

    public List<SalesReportProductLineDto> Products { get; set; } = new();
    public List<SalesReportWaiterLineDto> Waiters { get; set; } = new();
    public List<SalesReportCategoryLineDto> Categories { get; set; } = new();

    // Geri qaytarmalar — returns made within the period (by return date, like the shift Z report).
    public int ReturnCount { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal CashReturns { get; set; }
    public decimal CardReturns { get; set; }
    public decimal CreditReturns { get; set; }
    /// <summary>TotalRevenue minus returns in the period.</summary>
    public decimal NetRevenue { get; set; }
    public List<SalesReportReturnLineDto> ReturnedProducts { get; set; } = new();
}

public class SalesReportReturnLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class SalesReportProductLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesReportWaiterLineDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesReportCategoryLineDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
