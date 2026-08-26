using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public record CreateRestaurantSectionCommand(CreateRestaurantSectionRequest Request) : IRequest<RestaurantSectionResponse>;

public record UpdateRestaurantSectionCommand(UpdateRestaurantSectionRequest Request) : IRequest<RestaurantSectionResponse>;

public record DeleteRestaurantSectionCommand(int Id) : IRequest;
