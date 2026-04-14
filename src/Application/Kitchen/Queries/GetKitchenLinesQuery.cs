using Application.Kitchen.Dtos;
using MediatR;

namespace Application.Kitchen.Queries.GetKitchenLines;

public record GetKitchenLinesQuery(int RestaurantId) : IRequest<List<KitchenOrderLineResponse>>;