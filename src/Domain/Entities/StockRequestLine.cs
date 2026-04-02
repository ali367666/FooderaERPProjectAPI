using Domain.Common;

namespace Domain.Entities;

public class StockRequestLine : CompanyEntity<int>
{
    public int StockRequestId { get; set; }
    public StockRequest StockRequest { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }
}