using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Geri qaytarma — a (partial or full) return of an already paid sale, found by scanning the
/// receipt barcode. The original Order stays Paid; returned quantities are tracked on its lines.
/// </summary>
public class SaleReturn : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public string ReturnNumber { get; set; } = default!;

    /// <summary>Refunded the same way the sale was paid (cash out of the drawer, card, or debt reduction).</summary>
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public bool RestockItems { get; set; }
    public string? Reason { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<SaleReturnLine> Lines { get; set; } = new List<SaleReturnLine>();
}
