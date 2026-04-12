using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Commands.Update;

public record UpdateMenuCategoryCommand(
    int Id,
    int CompanyId,
    UpdateMenuCategoryRequest Request) : IRequest;