using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Queries.GetById;

public record GetMenuCategoryByIdQuery(
    int Id,
    int CompanyId) : IRequest<MenuCategoryResponse>;