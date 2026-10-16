using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public record UpdateMenuItemTypeCommand(UpdateMenuItemTypeRequest Request) : IRequest<MenuItemTypeResponse>;
