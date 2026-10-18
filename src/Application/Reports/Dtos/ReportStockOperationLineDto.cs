namespace Application.Reports.Dtos;
public class ReportStockOperationLineDto
{
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public decimal Received { get; set; }
    public decimal TransferIn { get; set; }
    public decimal TransferOut { get; set; }
    public decimal Consumed { get; set; }

    public decimal Returned { get; set; }
}
