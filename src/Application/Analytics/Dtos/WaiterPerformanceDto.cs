namespace Application.Analytics.Dtos;

public class WaiterPerformanceDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}
