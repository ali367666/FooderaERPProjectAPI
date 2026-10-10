using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class UpdateCompanyLicenseCommandHandler : IRequestHandler<UpdateCompanyLicenseCommand, CompanyLicenseDetailResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDeviceAccessService _access;

    public UpdateCompanyLicenseCommandHandler(ILicenseRepository repo, ICurrentUserService currentUser, IDeviceAccessService access)
    {
        _repo = repo;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<CompanyLicenseDetailResponse> Handle(UpdateCompanyLicenseCommand request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);
        var dto = request.Request;

        var license = await _repo.GetOrCreateAsync(request.CompanyId, cancellationToken);
        license.DeploymentType = Enum.TryParse<DeploymentType>(dto.DeploymentType, true, out var dt) ? dt : DeploymentType.Cloud;
        license.RemoteAccessEnabled = dto.RemoteAccessEnabled;
        if (license.RemoteAccessExpiresAtUtc != dto.RemoteAccessExpiresAtUtc)
            license.LastExpiryNoticeDays = null;
        license.RemoteAccessExpiresAtUtc = dto.RemoteAccessExpiresAtUtc;
        license.OfflineModeEnabled = dto.OfflineModeEnabled;
        license.MonthlyPrice = dto.MonthlyPrice;
        license.LastModifiedAtUtc = DateTime.UtcNow;
        license.LastModifiedByUserId = _currentUser.UserId;

        await _repo.SaveChangesAsync(cancellationToken);
        _access.Invalidate(request.CompanyId);
        return await LicenseMapping.DetailAsync(_repo, request.CompanyId, cancellationToken);
    }
}
