using Application.StoreMode.Fiscal;
using Domain.Entities.StoreMode;

namespace Infrastructure.Services;

/// <summary>
/// Stand-in until a fiscal device model and protocol are chosen: it reports that nothing is configured, so a sale
/// of a company that switched fiscal registration on shows status "Failed" with a clear reason instead of pretending.
/// </summary>
public sealed class NotConfiguredStoreFiscalDevice : IStoreFiscalDevice
{
    public Task<StoreFiscalResult> RegisterSaleAsync(StoreSale sale, CancellationToken ct)
        => Task.FromResult(new StoreFiscalResult(false, null, "Fiskal cihaz adapteri konfiqurasiya edilməyib."));
}
