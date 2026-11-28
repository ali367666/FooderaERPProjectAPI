namespace Application.Analytics.Dtos;

public class SalesReportWaiterLineDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Service charge collected on this waiter's orders — the amount owed to the waiter.</summary>
    public decimal ServiceCharge { get; set; }
    public decimal AverageCheck { get; set; }
    /// <summary>Place by revenue, 1 = the best.</summary>
    public int Rank { get; set; }
    /// <summary>Revenue against the best waiter's, 0–100 (the length of the comparison bar).</summary>
    public decimal PercentOfBest { get; set; }
    public decimal PreviousRevenue { get; set; }
    public decimal? ChangePercent { get; set; }
    /// <summary>Leader, Rising (sold more than in the earlier period) or Active.</summary>
    public string Status { get; set; } = "Active";
    /// <summary>The profile photo of the waiter's user account, if any.</summary>
    public string? PhotoUrl { get; set; }
}
