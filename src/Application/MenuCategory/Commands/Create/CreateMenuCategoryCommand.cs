using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Commands.Create;

public record CreateMenuCategoryCommand(
    CreateMenuCategoryRequest Request) : IRequest<int>;