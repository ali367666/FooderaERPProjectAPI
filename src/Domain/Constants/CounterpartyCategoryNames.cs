namespace Domain.Constants;

/// <summary>
/// The counterparty categories every company has: fixed by the system, so they can't be created,
/// renamed or deleted. "Bank" holds the payment banks.
/// </summary>
public static class CounterpartyCategoryNames
{
    public const string Supplier = "Tədarükçü";
    public const string Retail = "Pərakəndə";
    public const string Wholesale = "Satıcı/Topdan";
    public const string Bank = "Bank";
    public const string MainSupplier = "Əsas tədarükçü";
    public const string Branch = "Şöbə";
    public const string Personnel = "Personal";

    public static readonly string[] Fixed =
    [
        Supplier, Retail, Wholesale, Bank, MainSupplier, Branch, Personnel
    ];

    /// <summary>"Satış" and "Topdan" used to be two categories; they are now the one "Satıcı/Topdan".</summary>
    public static readonly string[] Merged = ["Satış", "Topdan"];

    public static bool IsFixed(string? name) =>
        Fixed.Any(f => string.Equals(f, name?.Trim(), StringComparison.OrdinalIgnoreCase));
}
