namespace Domain.Constants;

/// <summary>The numeric code a staff member signs in with (POS) and is identified by.</summary>
public static class StaffCodeRules
{
    public const int MinLength = 1;
    public const int MaxLength = 20;

    /// <summary>A rotating-PIN role types today's date (ddMM) in front of the code on the POS.</summary>
    public const int MaxLoginLength = MaxLength + 4;

    /// <summary>
    /// A restaurant's staff code is digits only (typed on a number pad). A store company's code may also hold Latin letters
    /// (typed on the on-screen or a real keyboard).
    /// </summary>
    public static bool IsValid(string code, bool allowLetters)
        => code.Length is >= MinLength and <= MaxLength
           && code.All(c => c is >= '0' and <= '9' || (allowLetters && c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')));
}
