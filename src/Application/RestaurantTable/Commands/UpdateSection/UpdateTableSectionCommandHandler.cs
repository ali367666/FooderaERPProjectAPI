using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTable.Commands.UpdateSection;

public class UpdateTableSectionCommandHandler : IRequestHandler<UpdateTableSectionCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTableSectionCommandHandler(IRestaurantTableRepository tableRepository, ICurrentUserService currentUserService)
    {
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantTableResponse> Handle(UpdateTableSectionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var table = await _tableRepository.GetByIdAsync(request.TableId, companyId, cancellationToken);
        if (table is null)
            throw new Exception("Masa tapılmadı.");

        table.SectionId = request.SectionId;
        _tableRepository.Update(table);
        await _tableRepository.SaveChangesAsync(cancellationToken);

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
            SectionId = table.SectionId
        };
    }
}
