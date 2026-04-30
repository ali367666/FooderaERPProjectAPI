using Application.Kitchen.Dtos;
using MediatR;

namespace Application.Kitchen.Queries;

public record GetKitchenOrdersQuery(int? CompanyId) : IRequest<List<KitchenOrderLineResponse>>;
