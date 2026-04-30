using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AppDbContext _dbContext;

    public JwtTokenService(
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository,
        AppDbContext dbContext)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _dbContext = dbContext;
    }

    public async Task<LoginResponse> CreateTokenAsync(
        User user,
        IEnumerable<string> permissions,
        IEnumerable<string>? roles = null,
        string? ipAddress = null)
    {
        var accessTokenExpiration = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

        var idStr = user.Id.ToString();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, idStr),
            new Claim("sub", idStr),
            new Claim("uid", idStr),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("companyId", user.CompanyId.ToString())
        };

        var effectivePermissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        var roleList = roles?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (roleList.Count > 0)
        {
            var roleIds = await _dbContext.Roles
                .AsNoTracking()
                .Where(x => roleList.Contains(x.Name!))
                .Select(x => x.Id)
                .ToListAsync();

            if (roleIds.Count > 0)
            {
                var rolePermissions = await _dbContext.RolePermissions
                    .AsNoTracking()
                    .Where(x => roleIds.Contains(x.RoleId))
                    .Select(x => x.Permission.Name)
                    .ToListAsync();

                foreach (var permission in rolePermissions)
                    effectivePermissions.Add(permission);
            }
        }

        foreach (var permission in effectivePermissions.Distinct())
        {
            claims.Add(new Claim("Permission", permission));
        }

        if (roleList.Count > 0)
        {
            foreach (var role in roleList)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
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
            RefreshTokenExpiration = refreshTokenExpiration,
            Roles = roleList,
            Permissions = effectivePermissions
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}