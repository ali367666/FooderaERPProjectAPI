using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class GetMyLicenseStatusQueryHandler : IRequestHandler<GetMyLicenseStatusQuery, LicenseStatusResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDeploymentInfo _deployment;

    public GetMyLicenseStatusQueryHandler(ILicenseRepository repo, ICurrentUserService currentUser, IDeploymentInfo deployment)
    {
        _repo = repo;
        _currentUser = currentUser;
        _deployment = deployment;
    }

    public async Task<LicenseStatusResponse> Handle(GetMyLicenseStatusQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;
        var license = await _repo.GetByCompanyIdAsync(companyId, cancellationToken);
        var response = new LicenseStatusResponse { IsLocalInstallation = _deployment.IsLocal };
        LicenseMapping.Fill(response, license, companyId, DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(request.DeviceKey))
        {
            var device = await _repo.GetActiveDeviceByKeyHashAsync(SecretHash.Sha256Hex(request.DeviceKey), cancellationToken);
            response.DeviceRegistered = device is not null && device.CompanyId == companyId;
        }

        return response;
    }
}
