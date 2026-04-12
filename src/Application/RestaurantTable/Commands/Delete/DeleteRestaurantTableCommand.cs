using MediatR;

namespace Application.RestaurantTables.Commands.Delete;

public class DeleteRestaurantTableCommand : IRequest
{
    public int Id { get; set; }
}