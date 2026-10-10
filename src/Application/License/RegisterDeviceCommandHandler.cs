using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

/// <summary>Anonymous: a restaurant device trades a one-time code for its permanent device key.</summary>
public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, RegisterDeviceResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly IDeviceAccessService _access;

    public RegisterDeviceCommandHandler(ILicenseRepository repo, IDeviceAccessService access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<RegisterDeviceResponse> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Kodu və cihazın adını daxil edin.");

        var now = DateTime.UtcNow;
        var code = await _repo.GetUsableRegistrationCodeAsync(
            SecretHash.Sha256Hex(SecretHash.NormalizeCode(dto.Code)), now, cancellationToken)
            ?? throw new BadRequestException("Kod yanlışdır, istifadə olunub və ya vaxtı bitib.");

        var key = SecretHash.NewDeviceKey();
        var device = new TrustedDevice
        {
            CompanyId = code.CompanyId,
            Name = name.Length > 100 ? name[..100] : name,
            KeyHash = SecretHash.Sha256Hex(key),
            IsActive = true,
            CreatedAtUtc = now,
            LastSeenAtUtc = now
        };

        await _repo.AddDeviceAsync(device, cancellationToken);
        code.UsedAtUtc = now;
        await _repo.SaveChangesAsync(cancellationToken);

        code.UsedByDeviceId = device.Id;
        await _repo.SaveChangesAsync(cancellationToken);
        _access.Invalidate(code.CompanyId);

        return new RegisterDeviceResponse { DeviceKey = key, DeviceName = device.Name, CompanyId = device.CompanyId };
    }
}
