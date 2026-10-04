using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Helpers;
using Application.Common.Responce;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, BaseResponse<LoginResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IAuthTokenIssuer authTokenIssuer,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _authTokenIssuer = authTokenIssuer;
        _logger = logger;
    }

    public async Task<BaseResponse<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation("Login attempt for {EmailOrUserName}", dto.EmailOrUserName);

        var user = await _userManager.FindByEmailAsync(dto.EmailOrUserName);
        user ??= await _userManager.FindByNameAsync(dto.EmailOrUserName);

        if (user is null)
        {
            _logger.LogWarning("Login failed. User not found for {EmailOrUserName}", dto.EmailOrUserName);
            return BaseResponse<LoginResponse>.Fail("Email/username or password is incorrect");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed. User account is disabled. UserId: {UserId}", user.Id);
            return BaseResponse<LoginResponse>.Fail("This account has been disabled. Contact an administrator.");
        }

        var passwordValid = await IsValidCredentialAsync(user, dto.Password ?? "");
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed. Wrong password for user {UserId}", user.Id);
            return BaseResponse<LoginResponse>.Fail("Email/username or password is incorrect");
        }

        // Checked after the password so the answer never reveals which accounts exist.
        if (!user.CanAccessAdminPanel)
        {
            _logger.LogWarning("Login failed. User {UserId} has no admin panel access", user.Id);
            return BaseResponse<LoginResponse>.Fail("Bu hesabın idarə panelinə girişi yoxdur — POS-a kodla daxil olun.");
        }

        var tokenResponse = await _authTokenIssuer.IssueForUserAsync(user, request.IpAddress, cancellationToken);

        _logger.LogInformation("Login successful for user {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Login successful");
    }

    /// <summary>
    /// Company staff sign in to the admin panel with their 4-digit POS code followed by today's
    /// month and day (Code + MMdd, e.g. 1234 on 4 October → 12341004). The SuperAdmin accounts —
    /// and older staff accounts that have no code yet — keep using their real password.
    /// </summary>
    private async Task<bool> IsValidCredentialAsync(Domain.Entities.User user, string submitted)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var usesPassword = string.IsNullOrEmpty(user.Code)
            || roles.Any(r => string.Equals(r, AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r, AppRoles.CompanySuperAdmin, StringComparison.OrdinalIgnoreCase));
        if (usesPassword)
            return await _userManager.CheckPasswordAsync(user, submitted);

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed. User {UserId} is locked out", user.Id);
            return false;
        }

        var expected = user.Code + BusinessTime.Now.ToString("MMdd");
        if (submitted.Trim() == expected)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
            return true;
        }

        // A 4-digit code + a public date is a short secret — lockout after repeated misses.
        await _userManager.AccessFailedAsync(user);
        return false;
    }
}
