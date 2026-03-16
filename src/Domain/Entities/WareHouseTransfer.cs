using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;


public class WarehouseTransfer : CompanyEntity<int>
{
    public int? StockRequestId { get; set; }
    public StockRequest? StockRequest { get; set; }

    public int FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = default!;

    public int ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = default!;

    // sürücünün maşını da anbar kimidir
    public int VehicleWarehouseId { get; set; }
    public Warehouse VehicleWarehouse { get; set; } = default!;

    public TransferStatus Status { get; set; }

    public ICollection<WarehouseTransferLine> Lines { get; set; } = new List<WarehouseTransferLine>();
}
