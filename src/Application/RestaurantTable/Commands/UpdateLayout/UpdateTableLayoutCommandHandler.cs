using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTable.Commands.UpdateLayout;

public class UpdateTableLayoutCommandHandler
    : IRequestHandler<UpdateTableLayoutCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTableLayoutCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantTableResponse> Handle(
        UpdateTableLayoutCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id, companyId, cancellationToken)
            ?? throw new Exception("Masa tapılmadı.");

        table.PosX = dto.PosX;
        table.PosY = dto.PosY;
        table.Width = dto.Width;
        table.Height = dto.Height;
        table.Shape = dto.Shape;
        table.Rotation = dto.Rotation;

        _restaurantTableRepository.Update(table);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        return new RestaurantTableResponse
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = table.Restaurant?.Name ?? string.Empty,
            Name = table.Name,
            Capacity = table.Capacity,
            IsActive = table.IsActive,
            IsOccupied = table.IsOccupied,
            PosX = table.PosX,
            PosY = table.PosY,
            Width = table.Width,
            Height = table.Height,
            Shape = table.Shape,
            Rotation = table.Rotation,
            HourlyRate = table.HourlyRate,
            Type = table.Type
        };
    }
}
