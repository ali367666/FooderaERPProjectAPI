using Application.Common.Interfaces;
using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetSalesLock;

/// <summary>Opens or closes changing sales (cancelling them) for a company. Only the platform SuperAdmin may do it.</summary>
public record SetSalesLockCommand(int CompanyId, bool Locked) : IRequest<BaseResponse>;

public class SetSalesLockCommandHandler : IRequestHandler<SetSalesLockCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;

    public SetSalesLockCommandHandler(ICompanyRepository companyRepository, ICompanySettingsRepository settings, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse> Handle(SetSalesLockCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsSuperAdmin)
            return BaseResponse.Fail("Bu əməliyyat yalnız SuperAdmin üçündür.");
        if (!await _companyRepository.ExistsAsync(request.CompanyId, ct))
            return BaseResponse.Fail("Company tapılmadı.");

        var settings = await _settings.GetByCompanyIdAsync(request.CompanyId, ct);
        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings { CompanyId = request.CompanyId };
            await _settings.AddAsync(settings, ct);
        }

        settings.SalesEditLocked = request.Locked;
        await _settings.SaveChangesAsync(ct);
        return BaseResponse.Ok(request.Locked ? "Satışların dəyişdirilməsi bağlandı." : "Satışların dəyişdirilməsi açıldı.");
    }
}
