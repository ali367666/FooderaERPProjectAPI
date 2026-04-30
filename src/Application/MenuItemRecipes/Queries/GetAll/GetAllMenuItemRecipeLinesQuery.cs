using Application.MenuItemRecipes.Dtos;
using MediatR;

namespace Application.MenuItemRecipes.Queries.GetAll;

public record GetAllMenuItemRecipeLinesQuery : IRequest<List<MenuItemRecipeResponse>>;
