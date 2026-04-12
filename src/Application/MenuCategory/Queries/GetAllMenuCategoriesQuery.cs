using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Queries.GetAll;

public record GetAllMenuCategoriesQuery() : IRequest<List<MenuCategoryResponse>>;