using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

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
