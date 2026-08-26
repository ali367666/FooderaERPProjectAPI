using Domain.Common;

namespace Domain.Entities;

public class Shift : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public int OpenedByUserId { get; set; }
    public User OpenedByUser { get; set; } = default!;
    public DateTime OpenedAt { get; set; }

    public int? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningCashAmount { get; set; }
    public decimal? ClosingCashAmount { get; set; }

    public bool IsOpen { get; set; } = true;
}
