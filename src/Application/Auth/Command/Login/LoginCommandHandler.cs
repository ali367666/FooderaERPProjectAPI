using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Responce;
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

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed. Wrong password for user {UserId}", user.Id);
            return BaseResponse<LoginResponse>.Fail("Email/username or password is incorrect");
        }

        var tokenResponse = await _authTokenIssuer.IssueForUserAsync(user, request.IpAddress, cancellationToken);

        _logger.LogInformation("Login successful for user {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Login successful");
    }
}
