using Application.RestaurantTable.Dtos.Request;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Commands.Update;

public class UpdateRestaurantTableCommand : IRequest<RestaurantTableResponse>
{
    public int Id { get; set; }
    public UpdateRestaurantTableRequest Request { get; set; } = default!;
}