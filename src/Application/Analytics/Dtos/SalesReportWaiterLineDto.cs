namespace Application.Analytics.Dtos;

public class SalesReportWaiterLineDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Service charge collected on this waiter's orders — the amount owed to the waiter.</summary>
    public decimal ServiceCharge { get; set; }
}
