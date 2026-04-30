using Application.MenuItemRecipes.Dtos;
using MediatR;

namespace Application.MenuItemRecipes.Commands.Upsert;

public record UpsertMenuItemRecipeCommand(int MenuItemId, UpsertMenuItemRecipeRequest Request)
    : IRequest<MenuItemRecipeResponse>;
