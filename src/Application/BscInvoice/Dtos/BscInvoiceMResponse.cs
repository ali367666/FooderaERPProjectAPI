namespace Application.BscInvoice.Dtos;

public class BscInvoiceMResponse
{
    public int Id { get; set; }
    public int BscInvoiceMId { get; set; }
    public string? DocNo { get; set; }
    public DateTime DocDate { get; set; }
    public int? EntityId { get; set; }
    public int? BranchId { get; set; }
    public int? CoId { get; set; }
    public decimal Amt { get; set; }
    public decimal AmtVat { get; set; }
    public int? PurchaseSales { get; set; }
    public DateTime? BscCreateDate { get; set; }
    public List<BscInvoiceDResponse> Lines { get; set; } = new();
}
