using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using Application.Restaurant.Queries;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

public class GetRestaurantByIdQueryHandler
    : IRequestHandler<GetRestaurantByIdQuery, BaseResponse<RestaurantResponse>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRestaurantByIdQueryHandler> _logger;

    public GetRestaurantByIdQueryHandler(
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetRestaurantByIdQueryHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<RestaurantResponse>> Handle(
        GetRestaurantByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting restaurant by id: {RestaurantId}", request.Id);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.Id, cancellationToken);

        if (restaurant is null || (!_currentUserService.IsSuperAdmin && restaurant.CompanyId != _currentUserService.CompanyId))
        {
            _logger.LogWarning("Restaurant not found. Id: {RestaurantId}", request.Id);
            return BaseResponse<RestaurantResponse>.Fail("Branch not found");
        }

        var response = _mapper.Map<RestaurantResponse>(restaurant);

        _logger.LogInformation("Restaurant found. Id: {RestaurantId}", request.Id);

        return BaseResponse<RestaurantResponse>.Ok(response, "Restaurant retrieved successfully");
    }
}