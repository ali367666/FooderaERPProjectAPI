namespace Application.Common.Interfaces;

public record BarcodeInfo(string Source, string Name, int Type, int Unit, bool ExistsInCompany, int? ExistingStockItemId);

/// <summary>Fills a new product from its barcode with what the system already knows (the company's own item, else the shared catalog).</summary>
public interface IBarcodeCatalog
{
    /// <summary>Null when nothing is known about the barcode.</summary>
    Task<BarcodeInfo?> LookupAsync(int companyId, string barcode, CancellationToken cancellationToken);

    /// <summary>Remembers a product entered by hand for the next company that scans the same barcode (the first entry stays).</summary>
    Task RememberAsync(string barcode, string name, int type, int unit, CancellationToken cancellationToken);
}
