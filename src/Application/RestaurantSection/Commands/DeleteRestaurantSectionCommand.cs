using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public record DeleteRestaurantSectionCommand(int Id) : IRequest;
