using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockPurchase : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public string SupplierName { get; set; } = default!;

    public bool IsImport { get; set; }

    public PurchaseCurrency Currency { get; set; } = PurchaseCurrency.AZN;

    // CBAR-dan çəkilən məzənnə (AZN/Currency). AZN alışı üçün 1.
    public decimal ExchangeRate { get; set; } = 1m;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public StockPurchaseStatus Status { get; set; } = StockPurchaseStatus.Draft;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? Note { get; set; }

    public ICollection<StockPurchaseLine> Lines { get; set; } = new List<StockPurchaseLine>();
}
