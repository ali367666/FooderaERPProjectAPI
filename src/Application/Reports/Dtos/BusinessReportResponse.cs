using Application.Analytics.Dtos;

namespace Application.Reports.Dtos;

public class BusinessReportResponse
{
    public string Period { get; set; } = default!;
    public DateTime FromLocal { get; set; }
    public DateTime ToLocal { get; set; }

    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    public int? RestaurantId { get; set; }

    public BusinessReportSummaryDto Summary { get; set; } = new();
    public BusinessReportSalesDto Sales { get; set; } = new();
    public BusinessReportIncomeDto Income { get; set; } = new();

    public List<SalesReportProductLineDto>? ProductSales { get; set; }

    public List<ReportCategoryGroupDto>? ProductSalesByCategory { get; set; }

    public List<ReportCounterpartyLineDto>? CounterpartyDetails { get; set; }

    public List<ReportExpenseLineDto>? Expenses { get; set; }

    public List<ReportOpenCheckDto>? OpenChecks { get; set; }

    public List<SalesReportGiftDto>? Gifts { get; set; }

    public List<SalesReportCancellationDto>? Cancellations { get; set; }

    public List<SalesReportTableLineDto>? TableSales { get; set; }

    public List<ReportStockOperationLineDto>? StockOperations { get; set; }

    public List<ReportStockBalanceLineDto>? StockBalance { get; set; }
}
