using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class StockItem : BaseEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public StockItemType Type { get; set; }
    public UnitOfMeasure Unit { get; set; }

    public int CategoryId { get; set; }
    public StockCategory Category { get; set; } = default!;

    // haranın stoku? səndə user restaurant/company var deyə bu vacibdir:
    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public int? RestaurantId { get; set; }     // baş ofis stok ola bilər
    public Restaurant? Restaurant { get; set; }

    // Sən “stok dataları” dedin — amma real stok rəqəmi adətən movement-lərdən hesablanır.
    // Minimum üçün saxlamaq olar:
    public decimal QuantityOnHand { get; set; }  // indi əlində neçə var
    public decimal? MinLevel { get; set; }       // alert üçün
}
