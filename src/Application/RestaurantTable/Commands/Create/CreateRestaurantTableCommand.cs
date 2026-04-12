using Application.RestaurantTable.Dtos.Request;
using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Commands.Create;

public class CreateRestaurantTableCommand : IRequest<RestaurantTableResponse>
{
    public CreateRestaurantTableRequest Request { get; set; } = default!;
}