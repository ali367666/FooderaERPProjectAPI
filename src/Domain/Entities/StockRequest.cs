using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class StockRequest : CompanyEntity<int>
{
    // tələb edən anbar (B)
    public int RequestingWarehouseId { get; set; }
    public Warehouse RequestingWarehouse { get; set; } = default!;

    // tələb edilən anbar (A və ya HeadOffice)
    public int SupplyingWarehouseId { get; set; }
    public Warehouse SupplyingWarehouse { get; set; } = default!;

    public StockRequestStatus Status { get; set; }

    public string? Note { get; set; }

    public ICollection<StockRequestLine> Lines { get; set; } = new List<StockRequestLine>();
}