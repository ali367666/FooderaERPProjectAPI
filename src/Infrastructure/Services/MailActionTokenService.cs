using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class MailActionTokenService : IMailActionTokenService
{
    private readonly IConfiguration _configuration;

    public MailActionTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int entityId, TimeSpan validity)
    {
        var expiresAtUnix = DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds();
        var signature = ComputeSignature(entityId, expiresAtUnix);
        return $"{expiresAtUnix}.{signature}";
    }

    public bool IsValid(int entityId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2) return false;
        if (!long.TryParse(parts[0], out var expiresAtUnix)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAtUnix) return false;

        var expected = Encoding.UTF8.GetBytes(ComputeSignature(entityId, expiresAtUnix));
        var actual = Encoding.UTF8.GetBytes(parts[1]);
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string ComputeSignature(int entityId, long expiresAtUnix)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!);
        var payload = Encoding.UTF8.GetBytes($"{entityId}.{expiresAtUnix}");
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(payload);
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
