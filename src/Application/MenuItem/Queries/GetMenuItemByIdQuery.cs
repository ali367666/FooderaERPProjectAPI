using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.GetById;

public record GetMenuItemByIdQuery(int Id) : IRequest<MenuItemResponse>;