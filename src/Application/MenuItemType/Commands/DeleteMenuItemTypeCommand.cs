using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public record DeleteMenuItemTypeCommand(int Id) : IRequest;
