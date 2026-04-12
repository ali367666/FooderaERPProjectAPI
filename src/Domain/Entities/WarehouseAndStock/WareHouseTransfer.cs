using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;


public class WarehouseTransfer : CompanyEntity<int>
{
    public int? StockRequestId { get; set; }
    public StockRequest? StockRequest { get; set; }

    public int FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = default!;

    public int ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = default!;

    public int? VehicleWarehouseId { get; set; }
    public Warehouse? VehicleWarehouse { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Draft;

    public string? Note { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;

    public ICollection<WarehouseTransferLine> Lines { get; set; } = new List<WarehouseTransferLine>();
}
