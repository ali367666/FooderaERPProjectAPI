using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetModules;

public class SetCompanyModulesCommandHandler
    : IRequestHandler<SetCompanyModulesCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;

    public SetCompanyModulesCommandHandler(
        ICompanyRepository companyRepository,
        ICompanySettingsRepository companySettingsRepository)
    {
        _companyRepository = companyRepository;
        _companySettingsRepository = companySettingsRepository;
    }

    public async Task<BaseResponse> Handle(SetCompanyModulesCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await _companyRepository.ExistsAsync(request.CompanyId, cancellationToken);
        if (!companyExists)
            return BaseResponse.Fail("Company tapılmadı.");

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings { CompanyId = request.CompanyId };
            await _companySettingsRepository.AddAsync(settings, cancellationToken);
        }

        var dto = request.Request;
        settings.ModuleFilial = dto.ModuleFilial;
        settings.ModuleAnbar = dto.ModuleAnbar;
        settings.ModuleRezervasyon = dto.ModuleRezervasyon;
        settings.ModuleMasaBolge = dto.ModuleMasaBolge;
        settings.ModulePaket = dto.ModulePaket;
        settings.ModuleOtel = dto.ModuleOtel;
        settings.ModuleFitnes = dto.ModuleFitnes;
        settings.ModuleDataSecimi = dto.ModuleDataSecimi;
        settings.ModuleQiymetSor = dto.ModuleQiymetSor;
        settings.ModuleKompleks = dto.ModuleKompleks;

        await _companySettingsRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Modullar yeniləndi.");
    }
}
