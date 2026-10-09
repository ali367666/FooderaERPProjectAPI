using Application.Auth.Dtos.Responce;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Helpers;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.PosLogin;

public sealed class PosLoginCommandHandler
    : IRequestHandler<PosLoginCommand, BaseResponse<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IStaffCodeResolver _staffCodeResolver;
    private readonly IWorkstationRepository _workstationRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly ILogger<PosLoginCommandHandler> _logger;

    public PosLoginCommandHandler(
        IUserRepository userRepository,
        IStaffCodeResolver staffCodeResolver,
        IWorkstationRepository workstationRepository,
        ICompanySettingsRepository companySettingsRepository,
        UserManager<Domain.Entities.User> userManager,
        IAuthTokenIssuer authTokenIssuer,
        ILogger<PosLoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _staffCodeResolver = staffCodeResolver;
        _workstationRepository = workstationRepository;
        _companySettingsRepository = companySettingsRepository;
        _userManager = userManager;
        _authTokenIssuer = authTokenIssuer;
        _logger = logger;
    }

    public async Task<BaseResponse<LoginResponse>> Handle(PosLoginCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(dto.CompanyId, cancellationToken);
        if (settings?.OpeningTime is { } openingTime && BusinessTime.Now.TimeOfDay < openingTime)
        {
            _logger.LogInformation(
                "POS login blocked before opening time. CompanyId: {CompanyId}, OpeningTime: {OpeningTime}",
                dto.CompanyId, openingTime);
            return BaseResponse<LoginResponse>.Fail($"Filial hələ açılmayıb. Açılış vaxtı: {openingTime:hh\\:mm}.");
        }

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

        if (dto.WorkstationId is > 0)
            await _workstationRepository.TouchLastSeenAsync(dto.WorkstationId.Value, dto.CompanyId, cancellationToken);

        var tokenResponse = await _authTokenIssuer.IssueForUserAsync(user, request.IpAddress, cancellationToken);

        _logger.LogInformation("POS login successful for user {UserId}", user.Id);

        return BaseResponse<LoginResponse>.Ok(tokenResponse, "Login successful");
    }

    private Task<Domain.Entities.User?> HandleCodeLoginAsync(int companyId, string submittedCode, CancellationToken cancellationToken)
        => _staffCodeResolver.ResolveAsync(companyId, submittedCode, cancellationToken);

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
