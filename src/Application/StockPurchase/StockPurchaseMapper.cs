using Application.StockPurchase.Dtos.Response;
using Domain.Enums;
using PurchaseEntity = Domain.Entities.WarehouseAndStock.StockPurchase;

namespace Application.StockPurchase;

/// <summary>The purchase as the API shows it, with its debt worked out from lines, returns and payments.</summary>
internal static class StockPurchaseMapper
{
    public static decimal Total(PurchaseEntity p) =>
        Math.Round(p.Lines.Sum(l => l.Quantity * l.UnitPriceAzn), 2, MidpointRounding.AwayFromZero);

    public static decimal ReturnedTotal(PurchaseEntity p) =>
        Math.Round(p.Returns.SelectMany(r => r.Lines).Sum(l => l.Quantity * l.UnitPriceAzn), 2, MidpointRounding.AwayFromZero);

    /// <summary>What is still owed to the supplier after returns and payments; zero unless the purchase is approved.</summary>
    public static decimal Remaining(PurchaseEntity p) =>
        p.Status == StockPurchaseStatus.Completed
            ? Math.Max(0, Total(p) - ReturnedTotal(p) - p.PaidAmountAzn)
            : 0;

    public static StockPurchaseResponse ToResponse(PurchaseEntity p, IReadOnlyDictionary<int, string> userNames)
    {
        string? Name(int? id) => id is { } v && userNames.TryGetValue(v, out var n) ? n : null;

        var returnedByLine = p.Returns.SelectMany(r => r.Lines)
            .GroupBy(l => l.StockPurchaseLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var lines = p.Lines.Select(l =>
        {
            var returned = returnedByLine.GetValueOrDefault(l.Id);
            return new StockPurchaseLineResponse
            {
                Id = l.Id,
                StockItemId = l.StockItemId,
                StockItemName = l.StockItem?.Name ?? string.Empty,
                Quantity = l.Quantity,
                ReturnedQuantity = returned,
                ReturnableQuantity = Math.Max(0, l.Quantity - returned),
                UnitPriceForeign = l.UnitPriceForeign,
                UnitPriceAzn = l.UnitPriceAzn,
                TotalForeign = l.Quantity * l.UnitPriceForeign,
                TotalAzn = l.Quantity * l.UnitPriceAzn,
            };
        }).ToList();

        var total = Total(p);
        var returnedTotal = ReturnedTotal(p);
        var net = total - returnedTotal;

        return new StockPurchaseResponse
        {
            Id = p.Id,
            DocumentNo = p.DocumentNo,
            CompanyId = p.CompanyId,
            CounterpartyId = p.CounterpartyId,
            CounterpartyName = p.Counterparty?.Name ?? string.Empty,
            IsImport = p.IsImport,
            Currency = p.Currency,
            CurrencyCode = p.Currency.ToString(),
            ExchangeRate = p.ExchangeRate,
            PurchaseDate = p.PurchaseDate,
            Status = p.Status,
            WarehouseId = p.WarehouseId,
            WarehouseName = p.Warehouse?.Name ?? string.Empty,
            Note = p.Note,
            CreatedAtUtc = p.CreatedAtUtc,
            CreatedByUserId = p.CreatedByUserId,
            CreatedByUserName = Name(p.CreatedByUserId),
            TotalForeign = lines.Sum(l => l.TotalForeign),
            TotalAzn = total,
            ReturnedAzn = returnedTotal,
            NetTotalAzn = net,
            SupplierInvoiceNo = p.SupplierInvoiceNo,
            ApprovedAtUtc = p.ApprovedAtUtc,
            CancelledAtUtc = p.CancelledAtUtc,
            PaidAmountAzn = p.PaidAmountAzn,
            RemainingAzn = Remaining(p),
            OverpaidAzn = p.Status == StockPurchaseStatus.Completed ? Math.Max(0, p.PaidAmountAzn - net) : 0,
            Lines = lines,
            Payments = p.Payments.OrderBy(x => x.CreatedAtUtc).Select(x => new StockPurchasePaymentResponse
            {
                Id = x.Id,
                Amount = x.Amount,
                Note = x.Note,
                PaidAtUtc = x.CreatedAtUtc,
                PaidByUserId = x.CreatedByUserId,
                PaidByUserName = Name(x.CreatedByUserId),
                RemainingAfterAzn = x.RemainingAfterAzn,
            }).ToList(),
            Returns = p.Returns.OrderBy(x => x.CreatedAtUtc).Select(r => new StockPurchaseReturnResponse
            {
                Id = r.Id,
                ReturnedAtUtc = r.CreatedAtUtc,
                ReturnedByUserId = r.CreatedByUserId,
                ReturnedByUserName = Name(r.CreatedByUserId),
                Note = r.Note,
                TotalAzn = Math.Round(r.Lines.Sum(l => l.Quantity * l.UnitPriceAzn), 2, MidpointRounding.AwayFromZero),
                Lines = r.Lines.Select(l => new StockPurchaseReturnLineResponse
                {
                    StockItemId = l.StockItemId,
                    StockItemName = l.StockItem?.Name ?? string.Empty,
                    Quantity = l.Quantity,
                    UnitPriceAzn = l.UnitPriceAzn,
                    TotalAzn = l.Quantity * l.UnitPriceAzn,
                }).ToList(),
            }).ToList(),
        };
    }
}
