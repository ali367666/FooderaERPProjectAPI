using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

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
