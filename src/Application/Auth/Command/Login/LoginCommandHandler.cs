using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, BaseResponse<LoginResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
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

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed. Wrong password for user {UserId}", user.Id);
            return BaseResponse<LoginResponse>.Fail("Email/username or password is incorrect");
        }

        var permissions = new HashSet<string>();

        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in userClaims.Where(x => x.Type == "Permission"))
        {
            permissions.Add(claim.Value);
        }

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims.Where(x => x.Type == "Permission"))
            {
                permissions.Add(claim.Value);
            }
        }

        var tokenResponse = await _jwtTokenService.CreateTokenAsync(user, permissions);

        _logger.LogInformation("Login successful for user {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Login successful");
    }
}