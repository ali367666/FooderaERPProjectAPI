using Domain.Enums;

namespace Application.StockMovements.Dtos.Response;

public class StockMovementListItemResponse
{
    public int CompanyId { get; set; }
    public int Id { get; set; }
    public string? SourceDocumentNo { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public UnitOfMeasure StockItemUnit { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public DateTime MovementDate { get; set; }
    public string? Note { get; set; }
}
