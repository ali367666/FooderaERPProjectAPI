using Application.Discounts.Dtos;
using MediatR;

namespace Application.Discounts.Queries;

public class GetAllDiscountsQuery : IRequest<List<DiscountResponse>>
{
}
