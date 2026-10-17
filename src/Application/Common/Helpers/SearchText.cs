namespace Application.Common.Helpers;

public static class SearchText
{
    /// <summary>Lower-cases and folds Azerbaijani letters so "toyuq" finds "Toyuq", "qehve" finds "Qəhvə", "aci" finds "Acı".</summary>
    public static string Fold(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var chars = value.Trim().ToLowerInvariant().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                'ı' or 'İ' => 'i',
                'ə' => 'e',
                'ö' => 'o',
                'ü' => 'u',
                'ş' => 's',
                'ç' => 'c',
                'ğ' => 'g',
                _ => chars[i]
            };
        }

        // "İ".ToLowerInvariant() is "i̇" (i + combining dot) — drop the combining mark.
        return new string(chars).Replace("̇", string.Empty);
    }
}
