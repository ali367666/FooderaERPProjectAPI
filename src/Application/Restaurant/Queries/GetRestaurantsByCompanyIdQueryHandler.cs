using Application.Common.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRestaurantsByCompanyIdQueryHandler> _logger;

    public GetRestaurantsByCompanyIdQueryHandler(
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetRestaurantsByCompanyIdQueryHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<RestaurantResponse>>> Handle(
        GetRestaurantsByCompanyIdQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin is always confined to their own company, regardless of what CompanyId was
        // requested in the URL — only SuperAdmin may target an arbitrary company.
        var companyId = _currentUserService.IsSuperAdmin
            ? request.CompanyId
            : _currentUserService.CompanyId;

        _logger.LogInformation(
            "Getting restaurants by company id: {CompanyId}",
            companyId);

        var restaurants = await _restaurantRepository
            .GetByCompanyIdAsync(companyId, cancellationToken);

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