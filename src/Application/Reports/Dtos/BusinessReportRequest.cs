using Domain.Enums;

namespace Application.Reports.Dtos;

/// <summary>
/// What the report page asks for: the period, the branch and which sections to include. The
/// summary and the sales / income panels are always returned; each section below is only
/// built (and returned, otherwise null) when its flag is on.
/// </summary>
public class BusinessReportRequest
{
    public ReportPeriod Period { get; set; } = ReportPeriod.Today;

    /// <summary>First day of a <see cref="ReportPeriod.Custom"/> period (the time part is ignored).</summary>
    public DateTime? From { get; set; }

    /// <summary>Last day (inclusive) of a <see cref="ReportPeriod.Custom"/> period; defaults to <see cref="From"/>.</summary>
    public DateTime? To { get; set; }

    /// <summary>Limit the report to one branch; all branches of the company when empty.</summary>
    public int? RestaurantId { get; set; }

    public bool ProductSales { get; set; } = true;
    public bool ProductSalesByCategory { get; set; } = true;
    public bool CounterpartyDetails { get; set; } = true;
    public bool Expenses { get; set; } = true;
    public bool OpenChecks { get; set; } = true;
    public bool Gifts { get; set; }
    public bool Cancellations { get; set; }
    public bool TableSales { get; set; }
    public bool StockOperations { get; set; } = true;
    public bool StockBalance { get; set; } = true;
}
