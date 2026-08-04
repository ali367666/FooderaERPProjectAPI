namespace Application.BscInvoice.Dtos;

public class BscInvoiceDResponse
{
    public int Id { get; set; }
    public int BscInvoiceDId { get; set; }
    public int BscInvoiceMId { get; set; }
    public int LineNo { get; set; }
    public int? ItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amt { get; set; }
    public decimal AmtVat { get; set; }
    public decimal VatRate { get; set; }
    public int? BranchId { get; set; }
    public int? CoId { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime? BscCreateDate { get; set; }
}
