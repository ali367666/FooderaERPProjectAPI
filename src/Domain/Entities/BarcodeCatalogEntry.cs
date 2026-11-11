using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// What the system knows about a barcode (name, type, unit) from the first company that entered the product by hand. It is shared by
/// all companies - only these identity fields, never a company's prices, stock or suppliers - and used to fill the next company's form
/// when the barcode is scanned. The first entry stays; nobody overwrites it.
/// </summary>
public class BarcodeCatalogEntry : BaseEntity<int>
{
    public string Barcode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Type { get; set; }
    public int Unit { get; set; }
}
