namespace Application.Analytics.Dtos;

public class HourlySalesDto
{
    public int Hour { get; set; }          // 0-23
    public string Label { get; set; } = default!;  // "09:00"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
