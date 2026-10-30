using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockPurchase : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = default!;

    public bool IsImport { get; set; }

    public PurchaseCurrency Currency { get; set; } = PurchaseCurrency.AZN;

    // CBAR-dan çəkilən məzənnə (AZN/Currency). AZN alışı üçün 1.
    public decimal ExchangeRate { get; set; } = 1m;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public StockPurchaseStatus Status { get; set; } = StockPurchaseStatus.Draft;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? Note { get; set; }

    /// <summary>The supplier's own invoice (qaimə) number, for matching the paper document.</summary>
    public string? SupplierInvoiceNo { get; set; }

    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>What has been paid to the supplier so far, in AZN. The rest of the total is still owed.</summary>
    public decimal PaidAmountAzn { get; set; }

    public ICollection<StockPurchasePayment> Payments { get; set; } = new List<StockPurchasePayment>();
    public ICollection<StockPurchaseReturn> Returns { get; set; } = new List<StockPurchaseReturn>();

    public ICollection<StockPurchaseLine> Lines { get; set; } = new List<StockPurchaseLine>();
}
