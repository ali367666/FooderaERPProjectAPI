using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public record CreateMenuItemTypeCommand(CreateMenuItemTypeRequest Request) : IRequest<MenuItemTypeResponse>;

public record UpdateMenuItemTypeCommand(UpdateMenuItemTypeRequest Request) : IRequest<MenuItemTypeResponse>;

public record DeleteMenuItemTypeCommand(int Id) : IRequest;
