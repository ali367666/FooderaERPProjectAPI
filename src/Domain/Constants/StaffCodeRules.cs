namespace Domain.Constants;

/// <summary>The numeric code a staff member signs in with (POS) and is identified by.</summary>
public static class StaffCodeRules
{
    public const int MinLength = 1;
    public const int MaxLength = 20;

    /// <summary>A rotating-PIN role types today's date (ddMM) in front of the code on the POS.</summary>
    public const int MaxLoginLength = MaxLength + 4;
}
