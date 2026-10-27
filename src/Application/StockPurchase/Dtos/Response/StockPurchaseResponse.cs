using Domain.Enums;

namespace Application.StockPurchase.Dtos.Response;

public class StockPurchaseResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int CompanyId { get; set; }
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = default!;
    public bool IsImport { get; set; }
    public PurchaseCurrency Currency { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal ExchangeRate { get; set; }
    public DateTime PurchaseDate { get; set; }
    public StockPurchaseStatus Status { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal TotalForeign { get; set; }
    public decimal TotalAzn { get; set; }
    public string? SupplierInvoiceNo { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    /// <summary>Paid to the supplier so far (AZN).</summary>
    public decimal PaidAmountAzn { get; set; }
    /// <summary>Still owed to the supplier (AZN); only an approved purchase owes anything.</summary>
    public decimal RemainingAzn { get; set; }
    public List<StockPurchaseLineResponse> Lines { get; set; } = new();
}
