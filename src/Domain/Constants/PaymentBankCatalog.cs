namespace Domain.Constants;

/// <summary>
/// The platform's payment banks (plus the QR code channel). A company has only the ones the SuperAdmin gave it;
/// the company's admin cannot create, rename or switch them.
/// </summary>
public static class PaymentBankCatalog
{
    public static readonly string[] Names = ["Kapital", "Paşa", "ABB", "Rabitəbank", "Unibank", "QR kod"];

    public static bool IsBankName(string? name)
        => !string.IsNullOrWhiteSpace(name) && Names.Any(n => string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase));
}
