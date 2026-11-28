namespace Application.Analytics.Dtos;

/// <summary>The totals of the earlier period the cards are compared with.</summary>
public class SalesReportPreviousDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public int OrderCount { get; set; }
}

/// <summary>One business day of the period, for the trend chart. Days without sales are listed with zeros.</summary>
public class SalesReportDayDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public int OrderCount { get; set; }
}

public class SalesReportPaymentShareDto
{
    /// <summary>Cash, Card or Credit.</summary>
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

/// <summary>A physical table of the floor plan with what it earned in the period (zero included), for the heat map.</summary>
public class SalesReportFloorTableDto
{
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }
    public int Capacity { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Shape { get; set; } = "square";
    public int Rotation { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Guests of the sittings where the number was entered.</summary>
    public int GuestCount { get; set; }
    /// <summary>Average guests per sitting against the capacity, 0–100; null when either is unknown.</summary>
    public decimal? OccupancyPercent { get; set; }
    /// <summary>High (half of the best table or more), Medium (a quarter or more), Low, or Empty.</summary>
    public string Heat { get; set; } = "Empty";
}
