using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class RevokeTrustedDeviceCommandHandler : IRequestHandler<RevokeTrustedDeviceCommand>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDeviceAccessService _access;

    public RevokeTrustedDeviceCommandHandler(ILicenseRepository repo, ICurrentUserService currentUser, IDeviceAccessService access)
    {
        _repo = repo;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task Handle(RevokeTrustedDeviceCommand request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);

        var device = await _repo.GetDeviceByIdAsync(request.DeviceId, cancellationToken)
            ?? throw new NotFoundException("Cihaz tapılmadı.");

        device.IsActive = false;
        device.LastModifiedAtUtc = DateTime.UtcNow;
        device.LastModifiedByUserId = _currentUser.UserId;
        await _repo.SaveChangesAsync(cancellationToken);
        _access.Invalidate(device.CompanyId);
    }
}
