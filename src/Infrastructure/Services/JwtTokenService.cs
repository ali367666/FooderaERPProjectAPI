using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public JwtTokenService(
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> CreateTokenAsync(
        User user,
        IEnumerable<string> permissions,
        string? ipAddress = null)
    {
        var accessTokenExpiration = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("CompanyId", user.CompanyId.ToString())
        };

        foreach (var permission in permissions.Distinct())
        {
            claims.Add(new Claim("Permission", permission));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: accessTokenExpiration,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        var refreshTokenValue = GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiration,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshToken, CancellationToken.None);
        await _refreshTokenRepository.SaveChangesAsync(CancellationToken.None);

        return new LoginResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiration = refreshTokenExpiration
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}