using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Queries.GetById;

public class GetRestaurantTableByIdQueryHandler
    : IRequestHandler<GetRestaurantTableByIdQuery, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRestaurantTableByIdQueryHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantTableResponse> Handle(GetRestaurantTableByIdQuery request, CancellationToken cancellationToken)
    {
        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id,
            _currentUserService.CompanyId,
            cancellationToken);

        if (table is null)
            throw new Exception("Masa tapılmadı.");

        return new RestaurantTableResponse
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = table.Restaurant?.Name ?? string.Empty,
            Name = table.Name,
            Capacity = table.Capacity,
            IsActive = table.IsActive,
            IsOccupied = table.IsOccupied
        };
    }
}