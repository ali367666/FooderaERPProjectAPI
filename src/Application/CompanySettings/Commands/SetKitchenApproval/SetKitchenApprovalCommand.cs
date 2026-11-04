using Application.Common.Interfaces;
using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetKitchenApproval;

/// <summary>Turns the kitchen acceptance step on or off for a company. Only the platform SuperAdmin may do it.</summary>
public record SetKitchenApprovalCommand(int CompanyId, bool Required) : IRequest<BaseResponse>;

public class SetKitchenApprovalCommandHandler : IRequestHandler<SetKitchenApprovalCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;

    public SetKitchenApprovalCommandHandler(ICompanyRepository companyRepository, ICompanySettingsRepository settings, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse> Handle(SetKitchenApprovalCommand request, CancellationToken ct)
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

        settings.KitchenApprovalRequired = request.Required;
        await _settings.SaveChangesAsync(ct);
        return BaseResponse.Ok(request.Required ? "Mətbəx təsdiqi aktiv edildi." : "Mətbəx təsdiqi söndürüldü.");
    }
}
