using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockRequest : CompanyEntity<int>
{
    public int RequestingWarehouseId { get; set; }
    public Warehouse RequestingWarehouse { get; set; } = default!;

    public int SupplyingWarehouseId { get; set; }
    public Warehouse SupplyingWarehouse { get; set; } = default!;

    public StockRequestStatus Status { get; set; } = StockRequestStatus.Draft;

    public string? Note { get; set; }

    public ICollection<StockRequestLine> Lines { get; set; } = new List<StockRequestLine>();
    public int? RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
}