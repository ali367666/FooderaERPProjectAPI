using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class BarcodeCatalog : IBarcodeCatalog
{
    private readonly AppDbContext _db;

    public BarcodeCatalog(AppDbContext db) => _db = db;

    public async Task<BarcodeInfo?> LookupAsync(int companyId, string barcode, CancellationToken cancellationToken)
    {
        var code = barcode.Trim();
        if (code.Length == 0) return null;

        // The company's own item first: the form then says "already in stock" instead of offering a copy.
        var own = await _db.StockItems.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Barcode == code)
            .Select(x => new { x.Id, x.Name, Type = (int)x.Type, Unit = (int)x.Unit })
            .FirstOrDefaultAsync(cancellationToken);
        if (own is not null)
            return new BarcodeInfo("company", own.Name, own.Type, own.Unit, true, own.Id);

        var entry = await _db.BarcodeCatalog.AsNoTracking()
            .Where(x => x.Barcode == code)
            .Select(x => new { x.Name, x.Type, x.Unit })
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is not null)
            return new BarcodeInfo("catalog", entry.Name, entry.Type, entry.Unit, false, null);

        // Products entered before the catalog existed: another company's item with this barcode teaches the same identity.
        var other = await _db.StockItems.AsNoTracking()
            .Where(x => x.Barcode == code)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, Type = (int)x.Type, Unit = (int)x.Unit })
            .FirstOrDefaultAsync(cancellationToken);
        return other is null ? null : new BarcodeInfo("catalog", other.Name, other.Type, other.Unit, false, null);
    }

    public async Task RememberAsync(string barcode, string name, int type, int unit, CancellationToken cancellationToken)
    {
        var code = barcode.Trim();
        if (code.Length == 0 || code.Length > 64 || string.IsNullOrWhiteSpace(name)) return;
        if (await _db.BarcodeCatalog.AnyAsync(x => x.Barcode == code, cancellationToken)) return;

        _db.BarcodeCatalog.Add(new BarcodeCatalogEntry { Barcode = code, Name = name.Trim(), Type = type, Unit = unit });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another company just added the same barcode: the first entry stays.
        }
    }
}
