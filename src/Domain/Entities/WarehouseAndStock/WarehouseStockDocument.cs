using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class WarehouseStockDocument : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public WarehouseStockDocumentStatus Status { get; set; } = WarehouseStockDocumentStatus.Draft;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public ICollection<WarehouseStockLine> Lines { get; set; } = new List<WarehouseStockLine>();
}
