using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using MediatR;

namespace Application.CompanySettings.Queries;

public class GetCompanySettingsBrandingQueryHandler
    : IRequestHandler<GetCompanySettingsBrandingQuery, BaseResponse<CompanySettingsBrandingResponse>>
{
    private readonly ICompanySettingsRepository _repository;

    public GetCompanySettingsBrandingQueryHandler(ICompanySettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<CompanySettingsBrandingResponse>> Handle(
        GetCompanySettingsBrandingQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _repository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        // Company has never opened the settings page yet — default every module to ON so
        // nothing disappears from the sidebar/POS until someone explicitly turns it off.
        var response = settings is null
            ? new CompanySettingsBrandingResponse
            {
                ModuleFilial = true,
                ModuleAnbar = true,
                ModuleRezervasyon = true,
                ModuleMasaBolge = true
            }
            : new CompanySettingsBrandingResponse
            {
                LoginLogoUrl = settings.LoginLogoUrl,
                ReportLogoUrl = settings.ReportLogoUrl,
                WallpaperUrl = settings.WallpaperUrl,
                LoginLocation = settings.LoginLocation,
                TransparencyLevel = settings.TransparencyLevel,
                FloorLabel = settings.FloorLabel,
                SocialLinks = settings.SocialLinks,
                Slogan = settings.Slogan,
                ProductColor = settings.ProductColor,
                ContactPhoneNumber = settings.ContactPhoneNumber,
                ReceiptFontSize = settings.ReceiptFontSize,
                CategoryFontSize = settings.CategoryFontSize,
                AllowReceiptEditAfterPrint = settings.AllowReceiptEditAfterPrint,
                WaiterCanPrintCustomerReceipt = settings.WaiterCanPrintCustomerReceipt,
                AlertMilliseconds = settings.AlertMilliseconds,
                AlertRingCount = settings.AlertRingCount,
                AlertRingIntervalSeconds = settings.AlertRingIntervalSeconds,
                ModuleFilial = settings.ModuleFilial,
                ModuleAnbar = settings.ModuleAnbar,
                ModuleRezervasyon = settings.ModuleRezervasyon,
                ModuleMasaBolge = settings.ModuleMasaBolge
            };

        return BaseResponse<CompanySettingsBrandingResponse>.Ok(response);
    }
}
