using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantSettings.Queries;

public class GetRestaurantModulesQueryHandler
    : IRequestHandler<GetRestaurantModulesQuery, BaseResponse<RestaurantModulesResponse>>
{
    private readonly IRestaurantSettingsRepository _restaurantSettingsRepository;

    public GetRestaurantModulesQueryHandler(IRestaurantSettingsRepository restaurantSettingsRepository)
    {
        _restaurantSettingsRepository = restaurantSettingsRepository;
    }

    public async Task<BaseResponse<RestaurantModulesResponse>> Handle(
        GetRestaurantModulesQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _restaurantSettingsRepository.GetByRestaurantIdAsync(request.RestaurantId, cancellationToken);

        var response = settings is null
            ? new RestaurantModulesResponse()
            : new RestaurantModulesResponse
            {
                ModuleAnbar = settings.ModuleAnbar,
                ModuleRezervasyon = settings.ModuleRezervasyon,
                ModuleMasaBolge = settings.ModuleMasaBolge,
                ModulePaket = settings.ModulePaket,
                ModuleOtel = settings.ModuleOtel,
                ModuleFitnes = settings.ModuleFitnes,
                ModuleDataSecimi = settings.ModuleDataSecimi,
                ModuleQiymetSor = settings.ModuleQiymetSor,
            };

        return BaseResponse<RestaurantModulesResponse>.Ok(response);
    }
}
