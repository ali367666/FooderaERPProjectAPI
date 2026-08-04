using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTable.Commands.UpdateLayout;

public class UpdateTableLayoutCommand : IRequest<RestaurantTableResponse>
{
    public int Id { get; set; }
    public UpdateTableLayoutRequest Request { get; set; } = default!;
}
