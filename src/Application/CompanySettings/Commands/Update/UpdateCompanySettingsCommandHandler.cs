using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using AutoMapper;
using MediatR;

namespace Application.CompanySettings.Commands.Update;

public class UpdateCompanySettingsCommandHandler
    : IRequestHandler<UpdateCompanySettingsCommand, BaseResponse<CompanySettingsResponse>>
{
    private readonly ICompanySettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UpdateCompanySettingsCommandHandler(
        ICompanySettingsRepository repository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CompanySettingsResponse>> Handle(
        UpdateCompanySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var settings = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);

        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings { CompanyId = companyId };
            await _repository.AddAsync(settings, cancellationToken);
        }

        settings.OpeningTime = dto.OpeningTime;

        settings.ModuleFilial = dto.ModuleFilial;
        settings.ModuleAnbar = dto.ModuleAnbar;
        settings.ModuleRezervasyon = dto.ModuleRezervasyon;
        settings.ModuleMasaBolge = dto.ModuleMasaBolge;

        settings.IntegrationWolt = dto.IntegrationWolt;
        settings.IntegrationBolt = dto.IntegrationBolt;
        settings.Integration189Delivery = dto.Integration189Delivery;

        settings.AlertMilliseconds = dto.AlertMilliseconds;
        settings.AlertRingCount = dto.AlertRingCount;
        settings.AlertRingIntervalSeconds = dto.AlertRingIntervalSeconds;

        settings.LoginLogoUrl = dto.LoginLogoUrl?.Trim();
        settings.ReportLogoUrl = dto.ReportLogoUrl?.Trim();
        settings.WallpaperUrl = dto.WallpaperUrl?.Trim();
        settings.LoginLocation = dto.LoginLocation?.Trim();
        settings.TransparencyLevel = dto.TransparencyLevel;
        settings.ProductColor = dto.ProductColor?.Trim();
        settings.FloorLabel = dto.FloorLabel?.Trim();
        settings.Slogan = dto.Slogan?.Trim();
        settings.SocialLinks = dto.SocialLinks?.Trim();
        settings.ContactPhoneNumber = dto.ContactPhoneNumber?.Trim();
        settings.ReceiptFontSize = dto.ReceiptFontSize;
        settings.CategoryFontSize = dto.CategoryFontSize;
        settings.AllowReceiptEditAfterPrint = dto.AllowReceiptEditAfterPrint;
        settings.WaiterCanPrintCustomerReceipt = dto.WaiterCanPrintCustomerReceipt;

        await _repository.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<CompanySettingsResponse>(settings);
        return BaseResponse<CompanySettingsResponse>.Ok(response, "Tənzimləmələr saxlanıldı.");
    }
}
