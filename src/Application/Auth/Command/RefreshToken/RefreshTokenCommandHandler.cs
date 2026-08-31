using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, BaseResponse<LoginResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        UserManager<Domain.Entities.User> userManager,
        IAuthTokenIssuer authTokenIssuer,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _authTokenIssuer = authTokenIssuer;
        _logger = logger;
    }

    public async Task<BaseResponse<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenValue = request.Request.RefreshToken;

        var existingToken = await _refreshTokenRepository.GetByTokenAsync(tokenValue, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            _logger.LogWarning("Refresh token yenilənmədi. Token tapılmadı və ya aktiv deyil.");
            return BaseResponse<LoginResponse>.Fail("Refresh token etibarsızdır. Yenidən daxil olun.");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Refresh token yenilənmədi. İstifadəçi tapılmadı və ya passivdir. UserId: {UserId}", existingToken.UserId);
            return BaseResponse<LoginResponse>.Fail("Refresh token etibarsızdır. Yenidən daxil olun.");
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedByIp = request.IpAddress;
        _refreshTokenRepository.Update(existingToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var tokenResponse = await _authTokenIssuer.IssueForUserAsync(user, request.IpAddress, cancellationToken);

        _logger.LogInformation("Refresh token uğurla yeniləndi. UserId: {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Token refreshed");
    }
}
