using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.PosLogin;

public sealed class PosLoginCommandHandler
    : IRequestHandler<PosLoginCommand, BaseResponse<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly ILogger<PosLoginCommandHandler> _logger;

    public PosLoginCommandHandler(
        IUserRepository userRepository,
        UserManager<Domain.Entities.User> userManager,
        IAuthTokenIssuer authTokenIssuer,
        ILogger<PosLoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _authTokenIssuer = authTokenIssuer;
        _logger = logger;
    }

    public async Task<BaseResponse<LoginResponse>> Handle(PosLoginCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var user = !string.IsNullOrWhiteSpace(dto.Code)
            ? await HandleCodeLoginAsync(dto.CompanyId, dto.Code, cancellationToken)
            : await HandleRfidLoginAsync(dto.CompanyId, dto.RfidCardId!, cancellationToken);

        if (user is null)
        {
            return BaseResponse<LoginResponse>.Fail("Invalid code or card");
        }

        if (dto.RestaurantId.HasValue && user.RestaurantId != dto.RestaurantId)
        {
            _logger.LogWarning("POS login failed. Restaurant mismatch for user {UserId}", user.Id);
            return BaseResponse<LoginResponse>.Fail("Invalid code or card");
        }

        if (!user.IsActive || !user.CanAccessFrontOffice)
        {
            _logger.LogWarning("POS login failed. User {UserId} inactive or lacks front office access", user.Id);
            return BaseResponse<LoginResponse>.Fail("Invalid code or card");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var tokenResponse = await _authTokenIssuer.IssueForUserAsync(user, request.IpAddress, cancellationToken);

        _logger.LogInformation("POS login successful for user {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Login successful");
    }

    private async Task<Domain.Entities.User?> HandleCodeLoginAsync(int companyId, string submittedCode, CancellationToken cancellationToken)
    {
        var codeCandidate = submittedCode[^4..];

        var user = await _userRepository.GetByCompanyAndCodeAsync(companyId, codeCandidate, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("POS login failed. No user found for CompanyId {CompanyId} and submitted code", companyId);
            return null;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("POS login failed. User {UserId} is locked out", user.Id);
            return null;
        }

        var expectedLength = user.CanAccessAdminPanel ? 8 : 4;
        var isValid = submittedCode.Length == expectedLength
            && (expectedLength == 4 || submittedCode[..4] == DateTime.Now.ToString("ddMM"));

        if (!isValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("POS login failed. Code mismatch for user {UserId}", user.Id);
            return null;
        }

        return user;
    }

    private async Task<Domain.Entities.User?> HandleRfidLoginAsync(int companyId, string rfidCardId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRfidCardIdAsync(rfidCardId, cancellationToken);
        if (user is null || user.CompanyId != companyId)
        {
            _logger.LogWarning("POS login failed. No matching RFID card for CompanyId {CompanyId}", companyId);
            return null;
        }

        return user;
    }
}
