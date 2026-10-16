using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PublicMenu.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.PublicMenu.Queries;

public record GetPublicMenuQuery(int RestaurantId) : IRequest<PublicMenuResponse?>;
