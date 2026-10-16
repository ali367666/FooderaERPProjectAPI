using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Licence keys look like "FOODERA-{payload}.{signature}" (both base64url): the payload is JSON,
/// the signature is ECDSA P-256 / SHA-256 over the payload text. Only the central server can
/// sign (private key); any installation can verify offline (public key).
/// </summary>
public class LicenseKeyService : ILicenseKeyService
{
    private const string Prefix = "FOODERA-";
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly LicensingOptions _options;

    public LicenseKeyService(IOptions<LicensingOptions> options)
    {
        _options = options.Value;
    }

    public bool CanIssue => !string.IsNullOrWhiteSpace(_options.PrivateKeyPem);

    public string Issue(LicenseKeyPayload payload)
    {
        if (!CanIssue)
            throw new InvalidOperationException("Licensing:PrivateKeyPem is not configured on this server.");

        var payloadPart = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, Json)));
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_options.PrivateKeyPem);
        var signature = ecdsa.SignData(Encoding.ASCII.GetBytes(payloadPart), HashAlgorithmName.SHA256);
        return $"{Prefix}{payloadPart}.{Base64Url(signature)}";
    }

    public LicenseKeyPayload? Verify(string key)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicKeyPem) || string.IsNullOrWhiteSpace(key))
            return null;

        var text = string.Concat(key.Where(c => !char.IsWhiteSpace(c)));
        if (!text.StartsWith(Prefix, StringComparison.Ordinal))
            return null;

        var parts = text[Prefix.Length..].Split('.');
        if (parts.Length != 2)
            return null;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_options.PublicKeyPem);
            if (!ecdsa.VerifyData(Encoding.ASCII.GetBytes(parts[0]), FromBase64Url(parts[1]), HashAlgorithmName.SHA256))
                return null;

            return JsonSerializer.Deserialize<LicenseKeyPayload>(FromBase64Url(parts[0]), Json);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException)
        {
            return null;
        }
    }

    /// <summary>For the one-time "--generate-license-keypair" command.</summary>
    public static (string PrivatePem, string PublicPem) GenerateKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (ecdsa.ExportECPrivateKeyPem(), ecdsa.ExportSubjectPublicKeyInfoPem());
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
