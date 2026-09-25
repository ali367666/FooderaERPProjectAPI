using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuthTokenIssuer : IAuthTokenIssuer
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthTokenIssuer> _logger;

    public AuthTokenIssuer(
        UserManager<User> userManager,
        RoleManager<AppRole> roleManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthTokenIssuer> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> IssueForUserAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in userClaims.Where(x => x.Type == "Permission"))
        {
            permissions.Add(claim.Value);
        }

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("Token roles for user {UserId}: {Roles}", user.Id, string.Join(", ", roles));

        // Role names are only unique WITHIN a company (or globally, for the null-CompanyId
        // SuperAdmin/template roles) — a plain FindByNameAsync would match an arbitrary
        // same-named role from a different tenant, leaking that tenant's permissions.
        var normalizedNames = roles.Select(r => r.ToUpperInvariant()).ToList();
        var candidateRoles = await _roleManager.Roles
            .Where(r => normalizedNames.Contains(r.NormalizedName!)
                && (r.CompanyId == user.CompanyId || r.CompanyId == null))
            .ToListAsync();

        foreach (var role in candidateRoles)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims.Where(x => x.Type == "Permission"))
            {
                permissions.Add(claim.Value);
            }
        }

        _logger.LogInformation(
            "Collected permission claims for user {UserId}: {Permissions}",
            user.Id,
            string.Join(", ", permissions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));

        var tokenResponse = await _jwtTokenService.CreateTokenAsync(user, permissions, roles, ipAddress);

        _logger.LogInformation(
            "JWT permissions for user {UserId}: {Permissions}",
            user.Id,
            string.Join(", ", tokenResponse.Permissions));

        return tokenResponse;
    }
}
