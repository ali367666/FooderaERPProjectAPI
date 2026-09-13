using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

public class GetAllRestaurantsQueryHandler
    : IRequestHandler<GetAllRestaurantsQuery, BaseResponse<List<RestaurantResponse>>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRestaurantsQueryHandler> _logger;

    public GetAllRestaurantsQueryHandler(
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetAllRestaurantsQueryHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<RestaurantResponse>>> Handle(
        GetAllRestaurantsQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin only ever sees their own company's branches — a bare "get all restaurants"
        // must never leak another tenant's data. SuperAdmin can target one company via CompanyId, or
        // omit it to browse every company (their existing "All Companies" cross-tenant view).
        List<Domain.Entities.Restaurant> restaurants;

        if (!_currentUserService.IsSuperAdmin)
        {
            _logger.LogInformation("Getting restaurants for own company: {CompanyId}", _currentUserService.CompanyId);
            restaurants = await _restaurantRepository.GetByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);
        }
        else if (request.CompanyId is > 0)
        {
            _logger.LogInformation("SuperAdmin getting restaurants for company: {CompanyId}", request.CompanyId);
            restaurants = await _restaurantRepository.GetByCompanyIdAsync(request.CompanyId.Value, cancellationToken);
        }
        else
        {
            _logger.LogInformation("SuperAdmin getting all restaurants across every company");
            restaurants = await _restaurantRepository.GetAllAsync(cancellationToken);
        }

        var response = _mapper.Map<List<RestaurantResponse>>(restaurants);

        _logger.LogInformation("Retrieved {Count} restaurants", response.Count);

        return BaseResponse<List<RestaurantResponse>>.Ok(response, "Restaurants retrieved successfully");
    }
}