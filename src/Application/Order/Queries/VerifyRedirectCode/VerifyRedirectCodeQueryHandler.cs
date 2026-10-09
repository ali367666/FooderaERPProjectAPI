using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Constants;
using MediatR;

namespace Application.Orders.Queries.VerifyRedirectCode;

public class VerifyRedirectCodeQueryHandler : IRequestHandler<VerifyRedirectCodeQuery, string>
{
    private readonly IStaffCodeResolver _staffCodeResolver;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public VerifyRedirectCodeQueryHandler(
        IStaffCodeResolver staffCodeResolver,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _staffCodeResolver = staffCodeResolver;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    /// <returns>The approver's user name.</returns>
    public async Task<string> Handle(VerifyRedirectCodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Kodu daxil edin.");

        var approver = await _staffCodeResolver.ResolveAsync(
            _currentUserService.CompanyId, request.Code.Trim(), cancellationToken);

        if (approver is null || !await _userRepository.HasPermissionAsync(approver.Id, AppPermissions.PosRedirectUser, cancellationToken))
            throw new BadRequestException("Kod yanlışdır və ya bu əməliyyat üçün icazəniz yoxdur.");

        return approver.UserName ?? approver.FullName ?? "";
    }
}
