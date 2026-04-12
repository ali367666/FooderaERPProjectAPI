using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Queries.GetAll;

public class GetAllRestaurantTablesQueryHandler
    : IRequestHandler<GetAllRestaurantTablesQuery, List<RestaurantTableResponse>>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllRestaurantTablesQueryHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<RestaurantTableResponse>> Handle(GetAllRestaurantTablesQuery request, CancellationToken cancellationToken)
    {
        var tables = await _restaurantTableRepository.GetAllAsync(
            _currentUserService.CompanyId,
            cancellationToken);

        return tables.Select(x => new RestaurantTableResponse
        {
            Id = x.Id,
            RestaurantId = x.RestaurantId,
            Name = x.Name,
            Capacity = x.Capacity,
            IsActive = x.IsActive,
            IsOccupied = x.IsOccupied
        }).ToList();
    }
}