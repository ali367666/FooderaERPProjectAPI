using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.RestaurantTables.Commands.Delete;

public class DeleteRestaurantTableCommandHandler : IRequestHandler<DeleteRestaurantTableCommand>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id,
            _currentUserService.CompanyId,
            cancellationToken);

        if (table is null)
            throw new Exception("Masa tapılmadı.");

        if (table.IsOccupied)
            throw new Exception("Dolu masa silinə bilməz.");

        _restaurantTableRepository.Delete(table);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);
    }
}