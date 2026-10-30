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
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public decimal TotalForeign { get; set; }

    /// <summary>What the purchase cost as invoiced (AZN), before any return.</summary>
    public decimal TotalAzn { get; set; }

    /// <summary>Value of the goods handed back to the supplier (AZN).</summary>
    public decimal ReturnedAzn { get; set; }

    /// <summary>The cost that counts: <see cref="TotalAzn"/> minus <see cref="ReturnedAzn"/>.</summary>
    public decimal NetTotalAzn { get; set; }

    public string? SupplierInvoiceNo { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>Paid to the supplier so far (AZN).</summary>
    public decimal PaidAmountAzn { get; set; }

    /// <summary>Still owed to the supplier (AZN); only an approved purchase owes anything.</summary>
    public decimal RemainingAzn { get; set; }

    /// <summary>Paid beyond the net cost (after a return): the supplier owes this back.</summary>
    public decimal OverpaidAzn { get; set; }

    public List<StockPurchaseLineResponse> Lines { get; set; } = new();
    public List<StockPurchasePaymentResponse> Payments { get; set; } = new();
    public List<StockPurchaseReturnResponse> Returns { get; set; } = new();
}

public class StockPurchasePaymentResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public int? PaidByUserId { get; set; }
    public string? PaidByUserName { get; set; }

    /// <summary>What was still owed right after this payment (AZN).</summary>
    public decimal RemainingAfterAzn { get; set; }
}

public class StockPurchaseReturnResponse
{
    public int Id { get; set; }
    public DateTime ReturnedAtUtc { get; set; }
    public int? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
    public string? Note { get; set; }
    public decimal TotalAzn { get; set; }
    public List<StockPurchaseReturnLineResponse> Lines { get; set; } = new();
}

public class StockPurchaseReturnLineResponse
{
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPriceAzn { get; set; }
    public decimal TotalAzn { get; set; }
}
