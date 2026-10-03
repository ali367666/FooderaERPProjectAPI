using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Helpers;

/// <summary>Device keys and registration codes are stored only as hashes.</summary>
public static class SecretHash
{
    // No 0/O, 1/I/L — codes are read out over the phone.
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant())));

    public static string NewDeviceKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>8-character code, shown as XXXX-XXXX.</summary>
    public static string NewRegistrationCode()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }

    /// <summary>Codes are compared without the dash and case-insensitively.</summary>
    public static string NormalizeCode(string code) => code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
}
