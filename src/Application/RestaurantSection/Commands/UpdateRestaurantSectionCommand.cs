using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public record UpdateRestaurantSectionCommand(UpdateRestaurantSectionRequest Request) : IRequest<RestaurantSectionResponse>;
