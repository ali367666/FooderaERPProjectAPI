using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Services;

public sealed class StaffCodeResolver : IStaffCodeResolver
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly ILogger<StaffCodeResolver> _logger;

    public StaffCodeResolver(
        IUserRepository userRepository,
        UserManager<Domain.Entities.User> userManager,
        ILogger<StaffCodeResolver> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Domain.Entities.User?> ResolveAsync(
        int companyId, string submittedCode, CancellationToken cancellationToken)
    {
        // Fixed code: the submitted value is the staff code itself. Rotating-PIN roles submit
        // ddMM + code, so the code is whatever follows the 4-char date prefix.
        var user = await _userRepository.GetByCompanyAndCodeAsync(companyId, submittedCode, cancellationToken);
        if (user is not null && await _userRepository.HasRotatingPinRoleAsync(user.Id, cancellationToken))
            user = null;

        if (user is null && submittedCode.Length > 4 && submittedCode[..4] == BusinessTime.Now.ToString("ddMM"))
        {
            var candidate = await _userRepository.GetByCompanyAndCodeAsync(companyId, submittedCode[4..], cancellationToken);
            if (candidate is not null && await _userRepository.HasRotatingPinRoleAsync(candidate.Id, cancellationToken))
                user = candidate;
        }

        if (user is null)
        {
            _logger.LogWarning("No user found for CompanyId {CompanyId} and submitted code", companyId);
            return null;
        }

        if (!user.IsActive || await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Staff code rejected. User {UserId} is inactive or locked out", user.Id);
            return null;
        }

        return user;
    }
}
