using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.RestaurantTables.Commands.Create;

public class CreateRestaurantTableCommandHandler
    : IRequestHandler<CreateRestaurantTableCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantTableResponse> Handle(CreateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var exists = await _restaurantTableRepository.ExistsByNameAsync(
            _currentUserService.CompanyId,
            dto.RestaurantId,
            dto.Name.Trim(),
            cancellationToken);

        if (exists)
            throw new Exception("Bu restoranda bu adda masa artıq mövcuddur.");

        var table = new Domain.Entities.RestaurantTable
        {
            CompanyId = _currentUserService.CompanyId,
            RestaurantId = dto.RestaurantId,
            Name = dto.Name.Trim(),
            Capacity = dto.Capacity,
            IsActive = true,
            IsOccupied = false
        };

        await _restaurantTableRepository.AddAsync(table, cancellationToken);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        return new RestaurantTableResponse
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            Name = table.Name,
            Capacity = table.Capacity,
            IsActive = table.IsActive,
            IsOccupied = table.IsOccupied
        };
    }
}