using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

/// <summary>
/// Local installation: anyone can paste a key — it only works if the platform signed it and its
/// company code matches a company on this installation.
/// </summary>
public class ImportLicenseKeyCommandHandler : IRequestHandler<ImportLicenseKeyCommand, LicenseStatusResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILicenseKeyService _keys;
    private readonly IDeploymentInfo _deployment;
    private readonly IDeviceAccessService _access;

    public ImportLicenseKeyCommandHandler(
        ILicenseRepository repo,
        ICompanyRepository companyRepository,
        ILicenseKeyService keys,
        IDeploymentInfo deployment,
        IDeviceAccessService access)
    {
        _repo = repo;
        _companyRepository = companyRepository;
        _keys = keys;
        _deployment = deployment;
        _access = access;
    }

    public async Task<LicenseStatusResponse> Handle(ImportLicenseKeyCommand request, CancellationToken cancellationToken)
    {
        if (!_deployment.IsLocal)
            throw new BadRequestException("Lisenziya açarı yalnız yerli quraşdırmada daxil edilir.");

        var raw = request.Request.LicenseKey ?? "";
        var payload = _keys.Verify(raw)
            ?? throw new BadRequestException("Lisenziya açarı yanlışdır və ya zədələnib.");

        var now = DateTime.UtcNow;
        if (payload.ExpiresAtUtc <= now)
            throw new BadRequestException("Bu açarın vaxtı artıq bitib — yeni açar istəyin.");

        var company = await _companyRepository.GetByCompanyCodeAsync(payload.CompanyCode, cancellationToken)
            ?? throw new BadRequestException($"Bu açar '{payload.CompanyCode}' şirkət kodu üçündür — bu quraşdırmada belə şirkət yoxdur.");

        var license = await _repo.GetOrCreateAsync(company.Id, cancellationToken);

        // Never let an older key overwrite a newer one.
        if (license.LicenseKeyExpiresAtUtc is { } current && current > payload.ExpiresAtUtc)
            throw new BadRequestException("Daxil edilən açar mövcud açardan köhnədir.");

        license.DeploymentType = DeploymentType.Local;
        license.LicenseKey = string.Concat(raw.Where(c => !char.IsWhiteSpace(c)));
        license.LicenseKeyExpiresAtUtc = payload.ExpiresAtUtc;
        license.RemoteAccessEnabled = payload.RemoteAccess;
        license.RemoteAccessExpiresAtUtc = payload.ExpiresAtUtc;
        license.OfflineModeEnabled = payload.OfflineMode;
        license.LastExpiryNoticeDays = null;
        license.LastModifiedAtUtc = now;

        await _repo.SaveChangesAsync(cancellationToken);
        _access.Invalidate(company.Id);

        var response = new LicenseStatusResponse { IsLocalInstallation = true };
        LicenseMapping.Fill(response, license, company.Id, now);
        return response;
    }
}
