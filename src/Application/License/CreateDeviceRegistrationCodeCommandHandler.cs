using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class CreateDeviceRegistrationCodeCommandHandler
    : IRequestHandler<CreateDeviceRegistrationCodeCommand, DeviceRegistrationCodeResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceRegistrationCodeCommandHandler(ILicenseRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<DeviceRegistrationCodeResponse> Handle(
        CreateDeviceRegistrationCodeCommand request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);

        var code = SecretHash.NewRegistrationCode();
        var now = DateTime.UtcNow;
        var entity = new DeviceRegistrationCode
        {
            CompanyId = request.CompanyId,
            CodeHash = SecretHash.Sha256Hex(SecretHash.NormalizeCode(code)),
            ExpiresAtUtc = now.AddHours(24),
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = now
        };

        await _repo.AddRegistrationCodeAsync(entity, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return new DeviceRegistrationCodeResponse { Code = code, ExpiresAtUtc = entity.ExpiresAtUtc };
    }
}
