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
    public List<SalesReportTableLineDto> Tables { get; set; } = new();
    /// <summary>Every paid order in the period, newest first.</summary>
    public List<SalesReportReceiptDto> Receipts { get; set; } = new();
    public decimal TotalServiceCharge { get; set; }
    /// <summary>Cancelled orders and removed products in the period, newest first.</summary>
    public List<SalesReportCancellationDto> Cancellations { get; set; } = new();
    /// <summary>Gift (complimentary) products on orders paid in the period, newest first.</summary>
    public List<SalesReportGiftDto> Gifts { get; set; } = new();

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
