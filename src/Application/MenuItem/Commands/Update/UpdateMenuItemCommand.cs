using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Commands.Update;

public record UpdateMenuItemCommand(
    int Id,
    UpdateMenuItemRequest Request) : IRequest;