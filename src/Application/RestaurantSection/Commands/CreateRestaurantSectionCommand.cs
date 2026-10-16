using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public record CreateRestaurantSectionCommand(CreateRestaurantSectionRequest Request) : IRequest<RestaurantSectionResponse>;
