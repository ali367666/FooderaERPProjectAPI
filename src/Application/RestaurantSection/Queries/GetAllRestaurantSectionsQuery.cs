using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Queries;

public record GetAllRestaurantSectionsQuery(int RestaurantId) : IRequest<List<RestaurantSectionResponse>>;
