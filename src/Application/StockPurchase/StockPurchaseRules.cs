using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StockPurchase.Dtos.Request;

namespace Application.StockPurchase;

/// <summary>Checks shared by creating and editing a purchase; returns the error text, or null when it is fine.</summary>
internal static class StockPurchaseRules
{
    public static async Task<string?> ValidateAsync(
        IStockPurchaseRepository repository,
        int companyId,
        int counterpartyId,
        int warehouseId,
        decimal exchangeRate,
        DateTime purchaseDate,
        IReadOnlyCollection<StockPurchaseLineRequest>? lines,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count == 0)
            return "Alış ən azı bir sətir içərməlidir.";
        if (lines.Any(x => x.Quantity <= 0))
            return "Bütün miqdarlar 0-dan böyük olmalıdır.";
        if (lines.Any(x => x.UnitPriceForeign < 0))
            return "Vahid qiymət mənfi ola bilməz.";
        if (exchangeRate <= 0)
            return "Məzənnə 0-dan böyük olmalıdır.";
        if (purchaseDate.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
            return "Alış tarixi gələcəkdə ola bilməz.";

        // Everything referenced must belong to the caller's own company.
        if (!await repository.WarehouseExistsAsync(companyId, warehouseId, cancellationToken))
            return "Anbar tapılmadı.";
        if (!await repository.CounterpartyExistsAsync(companyId, counterpartyId, cancellationToken))
            return "Təchizatçı (kontragent) tapılmadı.";

        var itemIds = lines.Select(l => l.StockItemId).Distinct().ToList();
        if (await repository.CountStockItemsAsync(companyId, itemIds, cancellationToken) != itemIds.Count)
            return "Sətirlərdəki stok məhsullarından biri tapılmadı.";

        return null;
    }

    public static string? CleanInvoiceNo(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
