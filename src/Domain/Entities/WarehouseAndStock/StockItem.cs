using System.Collections.Generic;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockItem : BaseEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public StockItemType Type { get; set; }
    public UnitOfMeasure Unit { get; set; }

    /// <summary>
    /// The warehouse's own sale price for this item — used as a POS fallback for any MenuItem
    /// linked to it whose own Station price hasn't been set.
    /// </summary>
    public decimal? SalePrice { get; set; }

    public int CategoryId { get; set; }
    public StockCategory Category { get; set; } = default!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();
    public ICollection<MenuItemRecipeLine> RecipeLines { get; set; } = new List<MenuItemRecipeLine>();
}
