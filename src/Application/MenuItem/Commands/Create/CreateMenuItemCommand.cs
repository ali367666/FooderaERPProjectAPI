using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Commands.Create;

public record CreateMenuItemCommand(
    CreateMenuItemRequest Request) : IRequest<int>;