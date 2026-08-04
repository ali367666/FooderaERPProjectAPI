using Domain.Enums;

namespace Application.StockPurchase.Dtos.Response;

public class StockPurchaseResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int CompanyId { get; set; }
    public string SupplierName { get; set; } = default!;
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
    public List<StockPurchaseLineResponse> Lines { get; set; } = new();
}
