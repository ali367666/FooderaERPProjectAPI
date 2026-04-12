using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Commands.Update;

public class UpdateRestaurantTableCommandHandler
    : IRequestHandler<UpdateRestaurantTableCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantTableResponse> Handle(UpdateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id,
            _currentUserService.CompanyId,
            cancellationToken);

        if (table is null)
            throw new Exception("Masa tapılmadı.");

        var exists = await _restaurantTableRepository.ExistsByNameAsync(
            _currentUserService.CompanyId,
            request.Request.RestaurantId,
            table.Id,
            request.Request.Name.Trim(),
            cancellationToken);

        if (exists)
            throw new Exception("Bu restoranda bu adda başqa masa artıq mövcuddur.");

        table.RestaurantId = request.Request.RestaurantId;
        table.Name = request.Request.Name.Trim();
        table.Capacity = request.Request.Capacity;
        table.IsActive = request.Request.IsActive;

        _restaurantTableRepository.Update(table);
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