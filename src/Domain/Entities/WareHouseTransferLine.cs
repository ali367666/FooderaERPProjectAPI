using Domain.Common;

namespace Domain.Entities;

public class WarehouseTransferLine : CompanyEntity<int>
{
    public int WarehouseTransferId { get; set; }
    public WarehouseTransfer WarehouseTransfer { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }
}