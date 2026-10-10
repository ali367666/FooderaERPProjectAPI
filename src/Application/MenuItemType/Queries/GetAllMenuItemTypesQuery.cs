using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemType.Commands;
using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Queries;

public record GetAllMenuItemTypesQuery : IRequest<List<MenuItemTypeResponse>>;
