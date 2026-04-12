using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.GetAll;

public record GetAllMenuItemsQuery() : IRequest<List<MenuItemResponse>>;