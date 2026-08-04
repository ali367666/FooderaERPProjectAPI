using Application.Analytics.Dtos;
using MediatR;

namespace Application.Analytics.Queries;

public class GetFoodCostQuery : IRequest<List<FoodCostResponse>>
{
}
