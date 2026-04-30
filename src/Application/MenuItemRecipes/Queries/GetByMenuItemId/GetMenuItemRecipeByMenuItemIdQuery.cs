using Application.MenuItemRecipes.Dtos;
using MediatR;

namespace Application.MenuItemRecipes.Queries.GetByMenuItemId;

public record GetMenuItemRecipeByMenuItemIdQuery(int MenuItemId) : IRequest<MenuItemRecipeResponse?>;
