using Application.Common.Interfaces;
using MediatR;

namespace Application.StockItem.Queries.LookupBarcode;

public class BarcodeLookupResponse
{
    public bool Found { get; set; }

    /// <summary>"company" (the company already has this item) or "catalog" (the system learned it from an earlier entry).</summary>
    public string? Source { get; set; }
    public string? Name { get; set; }
    public int? Type { get; set; }
    public int? Unit { get; set; }
    public bool ExistsInCompany { get; set; }
    public int? ExistingStockItemId { get; set; }
}

/// <summary>A scanned barcode: what the system already knows about the product, to fill the new item's form.</summary>
public record LookupStockItemBarcodeQuery(string Barcode, int? CompanyId = null) : IRequest<BarcodeLookupResponse>;

public class LookupStockItemBarcodeQueryHandler : IRequestHandler<LookupStockItemBarcodeQuery, BarcodeLookupResponse>
{
    private readonly IBarcodeCatalog _catalog;
    private readonly ICurrentUserService _currentUser;

    public LookupStockItemBarcodeQueryHandler(IBarcodeCatalog catalog, ICurrentUserService currentUser)
    {
        _catalog = catalog;
        _currentUser = currentUser;
    }

    public async Task<BarcodeLookupResponse> Handle(LookupStockItemBarcodeQuery request, CancellationToken ct)
    {
        // A SuperAdmin works inside the company they picked; everyone else inside their own.
        var companyId = _currentUser.IsSuperAdmin && request.CompanyId is > 0 ? request.CompanyId.Value : _currentUser.CompanyId;
        var info = await _catalog.LookupAsync(companyId, request.Barcode, ct);
        return info is null
            ? new BarcodeLookupResponse()
            : new BarcodeLookupResponse
            {
                Found = true,
                Source = info.Source,
                Name = info.Name,
                Type = info.Type,
                Unit = info.Unit,
                ExistsInCompany = info.ExistsInCompany,
                ExistingStockItemId = info.ExistingStockItemId,
            };
    }
}
