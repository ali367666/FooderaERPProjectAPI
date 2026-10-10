namespace Application.Shift.Dtos;

public class ShiftResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int OpenedByUserId { get; set; }
    public string? OpenedByUserName { get; set; }
    public DateTime OpenedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal? ClosingCashAmount { get; set; }
    public bool IsOpen { get; set; }
}
