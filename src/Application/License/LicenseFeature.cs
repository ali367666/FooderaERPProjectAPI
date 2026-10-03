using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

// ---------------------------------------------------------------- DTOs

public class LicenseStatusResponse
{
    public int CompanyId { get; set; }
    public string DeploymentType { get; set; } = default!;
    public bool RemoteAccessEnabled { get; set; }
    /// <summary>Online right now (enabled and not expired).</summary>
    public bool RemoteAccessActive { get; set; }
    public DateTime? RemoteAccessExpiresAtUtc { get; set; }
    public int? DaysLeft { get; set; }
    public bool OfflineModeEnabled { get; set; }
    /// <summary>The calling device is one of the company's registered devices.</summary>
    public bool DeviceRegistered { get; set; }

    /// <summary>This server is a Local installation (licensed by key).</summary>
    public bool IsLocalInstallation { get; set; }
    public DateTime? LicenseKeyExpiresAtUtc { get; set; }
    public bool LicenseKeyValid { get; set; }
}

public class CompanyLicenseDetailResponse : LicenseStatusResponse
{
    public decimal? MonthlyPrice { get; set; }
    public List<LicensePaymentResponse> Payments { get; set; } = new();
    public List<TrustedDeviceResponse> Devices { get; set; } = new();
}

public class LicensePaymentResponse
{
    public int Id { get; set; }
    public int Months { get; set; }
    public decimal Amount { get; set; }
    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class TrustedDeviceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

public class UpdateCompanyLicenseRequest
{
    public string DeploymentType { get; set; } = "Cloud";
    public bool RemoteAccessEnabled { get; set; }
    public DateTime? RemoteAccessExpiresAtUtc { get; set; }
    public bool OfflineModeEnabled { get; set; }
    public decimal? MonthlyPrice { get; set; }
}

public class ExtendLicenseRequest
{
    public int Months { get; set; } = 1;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class LicenseKeyResponse
{
    public string LicenseKey { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}

public class ImportLicenseKeyRequest
{
    public string LicenseKey { get; set; } = default!;
}

public class RegisterDeviceRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class RegisterDeviceResponse
{
    public string DeviceKey { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public int CompanyId { get; set; }
}

public class DeviceRegistrationCodeResponse
{
    public string Code { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}

// ---------------------------------------------------------------- Requests

public record GetMyLicenseStatusQuery(string? DeviceKey) : IRequest<LicenseStatusResponse>;
public record GetCompanyLicenseQuery(int CompanyId) : IRequest<CompanyLicenseDetailResponse>;
public record UpdateCompanyLicenseCommand(int CompanyId, UpdateCompanyLicenseRequest Request) : IRequest<CompanyLicenseDetailResponse>;
public record ExtendCompanyLicenseCommand(int CompanyId, ExtendLicenseRequest Request) : IRequest<CompanyLicenseDetailResponse>;
public record CreateDeviceRegistrationCodeCommand(int CompanyId) : IRequest<DeviceRegistrationCodeResponse>;
public record RevokeTrustedDeviceCommand(int DeviceId) : IRequest;
public record RegisterDeviceCommand(RegisterDeviceRequest Request) : IRequest<RegisterDeviceResponse>;
public record IssueLicenseKeyCommand(int CompanyId) : IRequest<LicenseKeyResponse>;
public record ImportLicenseKeyCommand(ImportLicenseKeyRequest Request) : IRequest<LicenseStatusResponse>;

// ---------------------------------------------------------------- Handlers

internal static class LicenseMapping
{
    public static void Fill(LicenseStatusResponse r, CompanyLicense? l, int companyId, DateTime now)
    {
        r.CompanyId = companyId;
        r.DeploymentType = (l?.DeploymentType ?? DeploymentType.Cloud).ToString();
        r.RemoteAccessEnabled = l?.RemoteAccessEnabled ?? false;
        r.RemoteAccessActive = l?.IsRemoteAccessActive(now) ?? false;
        r.RemoteAccessExpiresAtUtc = l?.RemoteAccessExpiresAtUtc;
        r.OfflineModeEnabled = l?.OfflineModeEnabled ?? false;
        r.LicenseKeyExpiresAtUtc = l?.LicenseKeyExpiresAtUtc;
        r.LicenseKeyValid = l?.IsLicenseKeyValid(now) ?? false;

        // Days left of whatever the company is counting down to (key on Local, Online in the cloud).
        var until = l?.DeploymentType == DeploymentType.Local ? l.LicenseKeyExpiresAtUtc : l?.RemoteAccessExpiresAtUtc;
        r.DaysLeft = until is { } u ? Math.Max(0, (int)Math.Ceiling((u - now).TotalDays)) : null;
    }

    public static async Task<CompanyLicenseDetailResponse> DetailAsync(
        ILicenseRepository repo, int companyId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var license = await repo.GetByCompanyIdAsync(companyId, ct);
        var response = new CompanyLicenseDetailResponse { MonthlyPrice = license?.MonthlyPrice };
        Fill(response, license, companyId, now);

        response.Payments = (await repo.GetPaymentsAsync(companyId, ct)).Select(p => new LicensePaymentResponse
        {
            Id = p.Id,
            Months = p.Months,
            Amount = p.Amount,
            PeriodFromUtc = p.PeriodFromUtc,
            PeriodToUtc = p.PeriodToUtc,
            Note = p.Note,
            CreatedByUserName = p.CreatedByUser?.FullName,
            CreatedAtUtc = p.CreatedAtUtc
        }).ToList();

        response.Devices = (await repo.GetDevicesAsync(companyId, ct)).Select(d => new TrustedDeviceResponse
        {
            Id = d.Id,
            Name = d.Name,
            IsActive = d.IsActive,
            CreatedAtUtc = d.CreatedAtUtc,
            LastSeenAtUtc = d.LastSeenAtUtc
        }).ToList();

        return response;
    }

    public static void EnsureSuperAdmin(ICurrentUserService currentUser)
    {
        if (!currentUser.IsSuperAdmin)
            throw new BadRequestException("Lisenziyanı yalnız platforma administratoru idarə edə bilər.");
    }
}

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

public class GetCompanyLicenseQueryHandler : IRequestHandler<GetCompanyLicenseQuery, CompanyLicenseDetailResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyLicenseQueryHandler(ILicenseRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<CompanyLicenseDetailResponse> Handle(GetCompanyLicenseQuery request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);
        return LicenseMapping.DetailAsync(_repo, request.CompanyId, cancellationToken);
    }
}

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

public class ExtendCompanyLicenseCommandHandler : IRequestHandler<ExtendCompanyLicenseCommand, CompanyLicenseDetailResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDeviceAccessService _access;

    public ExtendCompanyLicenseCommandHandler(ILicenseRepository repo, ICurrentUserService currentUser, IDeviceAccessService access)
    {
        _repo = repo;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<CompanyLicenseDetailResponse> Handle(ExtendCompanyLicenseCommand request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);
        var dto = request.Request;
        if (dto.Months is < 1 or > 36)
            throw new BadRequestException("Ay sayı 1 ilə 36 arasında olmalıdır.");
        if (dto.Amount < 0)
            throw new BadRequestException("Məbləğ mənfi ola bilməz.");

        var now = DateTime.UtcNow;
        var license = await _repo.GetOrCreateAsync(request.CompanyId, cancellationToken);

        // Paying early extends from the current end date; after expiry it starts from today.
        var from = license.RemoteAccessExpiresAtUtc is { } until && until > now ? until : now;
        var to = from.AddMonths(dto.Months);

        license.RemoteAccessEnabled = true;
        license.RemoteAccessExpiresAtUtc = to;
        license.LastExpiryNoticeDays = null;
        license.LastModifiedAtUtc = now;
        license.LastModifiedByUserId = _currentUser.UserId;

        await _repo.AddPaymentAsync(new LicensePayment
        {
            CompanyId = request.CompanyId,
            Months = dto.Months,
            Amount = dto.Amount,
            PeriodFromUtc = from,
            PeriodToUtc = to,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = now
        }, cancellationToken);

        await _repo.SaveChangesAsync(cancellationToken);
        _access.Invalidate(request.CompanyId);
        return await LicenseMapping.DetailAsync(_repo, request.CompanyId, cancellationToken);
    }
}

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
