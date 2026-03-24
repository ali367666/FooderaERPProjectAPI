using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

public class GetRestaurantsByCompanyIdQueryHandler
    : IRequestHandler<GetRestaurantsByCompanyIdQuery, BaseResponse<List<RestaurantResponse>>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRestaurantsByCompanyIdQueryHandler> _logger;

    public GetRestaurantsByCompanyIdQueryHandler(
        IRestaurantRepository restaurantRepository,
        IMapper mapper,
        ILogger<GetRestaurantsByCompanyIdQueryHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<RestaurantResponse>>> Handle(
        GetRestaurantsByCompanyIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting restaurants by company id: {CompanyId}",
            request.CompanyId);

        var restaurants = await _restaurantRepository
            .GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        var response = _mapper.Map<List<RestaurantResponse>>(restaurants);

        _logger.LogInformation(
            "Retrieved {Count} restaurants for company id: {CompanyId}",
            response.Count,
            request.CompanyId);

        return BaseResponse<List<RestaurantResponse>>.Ok(
            response,
            "Restaurants retrieved successfully");
    }
}