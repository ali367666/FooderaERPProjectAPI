using MediatR;

namespace Application.MenuItems.Commands.Delete;

public record DeleteMenuItemCommand(int Id) : IRequest;