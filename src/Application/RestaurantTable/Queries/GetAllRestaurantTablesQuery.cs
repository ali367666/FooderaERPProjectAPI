using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTables.Queries.GetAll;

public class GetAllRestaurantTablesQuery : IRequest<List<RestaurantTableResponse>>
{
}