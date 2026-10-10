using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public record CreateMenuItemTypeCommand(CreateMenuItemTypeRequest Request) : IRequest<MenuItemTypeResponse>;
