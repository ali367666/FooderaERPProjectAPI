namespace Application.Analytics.Dtos;

public class DailyRevenueDto
{
    public string Day { get; set; } = default!;   // "Baz", "B.e", "Ça", "Çə", "Cü", "Şn", "Şb"
    public string Date { get; set; } = default!;  // "15.06"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
