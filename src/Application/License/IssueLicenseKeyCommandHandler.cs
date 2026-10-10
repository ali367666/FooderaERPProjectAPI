using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

/// <summary>
/// Central server: signs a licence key for a Local installation, valid until the company's
/// paid-until date ("Ödənildi, uzat" moves that date; issue a fresh key after each payment).
/// </summary>
public class IssueLicenseKeyCommandHandler : IRequestHandler<IssueLicenseKeyCommand, LicenseKeyResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILicenseKeyService _keys;
    private readonly ICurrentUserService _currentUser;

    public IssueLicenseKeyCommandHandler(
        ILicenseRepository repo,
        ICompanyRepository companyRepository,
        ILicenseKeyService keys,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _companyRepository = companyRepository;
        _keys = keys;
        _currentUser = currentUser;
    }

    public async Task<LicenseKeyResponse> Handle(IssueLicenseKeyCommand request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);
        if (!_keys.CanIssue)
            throw new BadRequestException("Bu serverdə lisenziya açarı yaratmaq üçün gizli açar (Licensing:PrivateKeyPem) qurulmayıb.");

        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Şirkət tapılmadı.");
        var license = await _repo.GetOrCreateAsync(request.CompanyId, cancellationToken);

        var expires = license.RemoteAccessExpiresAtUtc
            ?? throw new BadRequestException("Əvvəlcə ödənişi qeyd edin (Ödənildi, uzat) — açar ödənilmiş tarixə qədər verilir.");
        if (expires <= DateTime.UtcNow)
            throw new BadRequestException("Ödənilmiş müddət bitib — əvvəlcə uzadın.");

        var key = _keys.Issue(new LicenseKeyPayload(
            company.CompanyCode,
            company.Name,
            license.DeploymentType.ToString(),
            license.RemoteAccessEnabled,
            license.OfflineModeEnabled,
            expires,
            DateTime.UtcNow));

        license.LicenseKey = key;
        license.LicenseKeyExpiresAtUtc = expires;
        license.LastExpiryNoticeDays = null;
        await _repo.SaveChangesAsync(cancellationToken);

        return new LicenseKeyResponse { LicenseKey = key, ExpiresAtUtc = expires };
    }
}
