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
        // The submitted value is the staff code itself — no date prefix, the code never changes by itself.
        var user = await _userRepository.GetByCompanyAndCodeAsync(companyId, submittedCode, cancellationToken);

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
