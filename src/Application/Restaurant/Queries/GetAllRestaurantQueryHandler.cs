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
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRestaurantsQueryHandler> _logger;

    public GetAllRestaurantsQueryHandler(
        IRestaurantRepository restaurantRepository,
        IMapper mapper,
        ILogger<GetAllRestaurantsQueryHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<RestaurantResponse>>> Handle(
        GetAllRestaurantsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all restaurants");

        var restaurants = await _restaurantRepository.GetAllAsync(cancellationToken);

        var response = _mapper.Map<List<RestaurantResponse>>(restaurants);

        _logger.LogInformation("Retrieved {Count} restaurants", response.Count);

        return BaseResponse<List<RestaurantResponse>>.Ok(response, "Restaurants retrieved successfully");
    }
}