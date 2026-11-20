using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.CompanySettings.Commands.SetStoreCostMethod;

/// <summary>
/// Sets how a company's store warehouse goods are costed: weighted average (off), FIFO or LIFO. Only the platform SuperAdmin may do it.
/// Switching FIFO / LIFO on makes opening cost layers from the stock that is there now; from then on every delivery is a layer.
/// </summary>
public record SetStoreCostMethodCommand(int CompanyId, StoreCostMethod Method) : IRequest<BaseResponse>;

public class SetStoreCostMethodCommandHandler : IRequestHandler<SetStoreCostMethodCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanySettingsRepository _settings;
    private readonly IStoreCostingRepository _costing;
    private readonly ICurrentUserService _currentUser;

    public SetStoreCostMethodCommandHandler(
        ICompanyRepository companyRepository, ICompanySettingsRepository settings, IStoreCostingRepository costing, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _settings = settings;
        _costing = costing;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse> Handle(SetStoreCostMethodCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsSuperAdmin)
            return BaseResponse.Fail("Bu əməliyyat yalnız SuperAdmin üçündür.");
        if (!Enum.IsDefined(request.Method))
            return BaseResponse.Fail("Maya metodu yanlışdır.");
        if (!await _companyRepository.ExistsAsync(request.CompanyId, ct))
            return BaseResponse.Fail("Company tapılmadı.");

        var settings = await _settings.GetByCompanyIdAsync(request.CompanyId, ct);
        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings { CompanyId = request.CompanyId };
            await _settings.AddAsync(settings, ct);
        }

        var before = settings.StoreCostMethod;
        await _costing.PrepareMethodChangeAsync(request.CompanyId, before, request.Method, ct);
        settings.StoreCostMethod = request.Method;
        await _settings.SaveChangesAsync(ct);

        return BaseResponse.Ok(request.Method switch
        {
            StoreCostMethod.Fifo => "FIFO işə salındı: satışın maya dəyəri ən köhnə alış qatından götürülür.",
            StoreCostMethod.Lifo => "LIFO işə salındı: satışın maya dəyəri ən yeni alış qatından götürülür.",
            _ => "FIFO / LIFO söndürüldü: orta maya ilə hesablanır.",
        });
    }
}
