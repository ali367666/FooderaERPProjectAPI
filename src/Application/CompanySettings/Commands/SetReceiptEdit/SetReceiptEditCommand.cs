using Application.Common.Interfaces;
using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetReceiptEdit;

/// <summary>Opens or closes correcting paid receipts for a company. Only the platform SuperAdmin may do it.</summary>
public record SetReceiptEditCommand(int CompanyId, bool Allowed) : IRequest<BaseResponse>;

public class SetReceiptEditCommandHandler : IRequestHandler<SetReceiptEditCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;

    public SetReceiptEditCommandHandler(ICompanyRepository companyRepository, ICompanySettingsRepository settings, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse> Handle(SetReceiptEditCommand request, CancellationToken ct)
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

        settings.ReceiptEditAllowed = request.Allowed;
        await _settings.SaveChangesAsync(ct);
        return BaseResponse.Ok(request.Allowed ? "Qəbz düzəlişi açıldı." : "Qəbz düzəlişi bağlandı.");
    }
}
