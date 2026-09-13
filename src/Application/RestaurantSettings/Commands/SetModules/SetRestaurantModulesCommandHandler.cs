using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantSettings.Commands.SetModules;

public class SetRestaurantModulesCommandHandler
    : IRequestHandler<SetRestaurantModulesCommand, BaseResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly IRestaurantSettingsRepository _restaurantSettingsRepository;

    public SetRestaurantModulesCommandHandler(
        IRestaurantRepository restaurantRepository,
        ICompanySettingsRepository companySettingsRepository,
        IRestaurantSettingsRepository restaurantSettingsRepository)
    {
        _restaurantRepository = restaurantRepository;
        _companySettingsRepository = companySettingsRepository;
        _restaurantSettingsRepository = restaurantSettingsRepository;
    }

    public async Task<BaseResponse> Handle(SetRestaurantModulesCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
            return BaseResponse.Fail("Filial tapılmadı.");

        var companySettings = await _companySettingsRepository.GetByCompanyIdAsync(restaurant.CompanyId, cancellationToken);
        if (companySettings?.ModuleKompleks != true)
        {
            return BaseResponse.Fail(
                "Filial-səviyyəli modullar yalnız \"İstirahət Kompleksi\" rejimindəki şirkətlər üçün mövcuddur.");
        }

        var settings = await _restaurantSettingsRepository.GetByRestaurantIdAsync(request.RestaurantId, cancellationToken);
        if (settings is null)
        {
            settings = new Domain.Entities.RestaurantSettings { RestaurantId = request.RestaurantId };
            await _restaurantSettingsRepository.AddAsync(settings, cancellationToken);
        }

        var dto = request.Request;
        settings.ModuleAnbar = dto.ModuleAnbar;
        settings.ModuleRezervasyon = dto.ModuleRezervasyon;
        settings.ModuleMasaBolge = dto.ModuleMasaBolge;
        settings.ModulePaket = dto.ModulePaket;
        settings.ModuleOtel = dto.ModuleOtel;
        settings.ModuleFitnes = dto.ModuleFitnes;
        settings.ModuleDataSecimi = dto.ModuleDataSecimi;
        settings.ModuleQiymetSor = dto.ModuleQiymetSor;

        await _restaurantSettingsRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Filial modulları yeniləndi.");
    }
}
