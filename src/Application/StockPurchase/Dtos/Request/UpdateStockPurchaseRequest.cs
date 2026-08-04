using Domain.Enums;

namespace Application.StockPurchase.Dtos.Request;

public class UpdateStockPurchaseRequest
{
    public string SupplierName { get; set; } = default!;
    public bool IsImport { get; set; }
    public PurchaseCurrency Currency { get; set; } = PurchaseCurrency.AZN;
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime PurchaseDate { get; set; }
    public int WarehouseId { get; set; }
    public string? Note { get; set; }
    public List<StockPurchaseLineRequest> Lines { get; set; } = new();
}
