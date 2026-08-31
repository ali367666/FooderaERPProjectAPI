using Domain.Enums;

namespace Application.StockPurchase.Dtos.Request;

public class CreateStockPurchaseRequest
{
    public int CounterpartyId { get; set; }
    public bool IsImport { get; set; }
    public PurchaseCurrency Currency { get; set; } = PurchaseCurrency.AZN;
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public string? Note { get; set; }
    public List<StockPurchaseLineRequest> Lines { get; set; } = new();
}
