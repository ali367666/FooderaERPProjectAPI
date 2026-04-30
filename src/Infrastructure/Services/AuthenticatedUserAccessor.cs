using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Infrastructure.Services;

public class AuthenticatedUserAccessor : IAuthenticatedUserAccessor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthenticatedUserAccessor> _logger;

    public AuthenticatedUserAccessor(
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        ILogger<AuthenticatedUserAccessor> logger)
    {
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<int> ResolveUserIdAsync(CancellationToken cancellationToken = default)
    {
        var fromClaims = _currentUserService.UserId;
        if (fromClaims > 0)
            return fromClaims;

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return 0;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = await _userManager.FindByEmailAsync(email);
            if (byEmail != null)
            {
                _logger.LogInformation(
                    "Resolved UserId {UserId} from email claim (claims had no id).",
                    byEmail.Id);
                return byEmail.Id;
            }
        }

        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var byName = await _userManager.FindByNameAsync(name);
            if (byName != null)
            {
                _logger.LogInformation(
                    "Resolved UserId {UserId} from Name claim (claims had no id).",
                    byName.Id);
                return byName.Id;
            }
        }

        _logger.LogWarning("Could not resolve UserId: no numeric id in claims and no user match for email/name.");
        return 0;
    }
}
