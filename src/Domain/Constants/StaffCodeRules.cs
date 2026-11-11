namespace Domain.Constants;

/// <summary>The numeric code a staff member signs in with (POS) and is identified by.</summary>
public static class StaffCodeRules
{
    public const int MinLength = 1;
    public const int MaxLength = 20;

    /// <summary>
    /// A store (magaza) staff code can be a scanned card's barcode: the card is read and its digits are the code, so a store
    /// company's code has no practical limit (450 characters is the most the unique index can hold).
    /// </summary>
    public const int StoreMaxLength = 450;

    public const int MaxLoginLength = MaxLength;

    /// <summary>
    /// A restaurant's staff code is digits only (typed on a number pad). A store company's code may also hold Latin letters
    /// (typed on the on-screen or a real keyboard).
    /// </summary>
    public static bool IsValid(string code, bool allowLetters)
        => code.Length >= MinLength && code.Length <= (allowLetters ? StoreMaxLength : MaxLength)
           && code.All(c => c is >= '0' and <= '9' || (allowLetters && c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')));
}
