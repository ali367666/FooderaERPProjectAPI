using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Queries.GetById;

public class GetRestaurantTableByIdQuery : IRequest<RestaurantTableResponse>
{
    public int Id { get; set; }
}