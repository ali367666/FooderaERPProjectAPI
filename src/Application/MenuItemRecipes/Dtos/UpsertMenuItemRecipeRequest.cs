namespace Application.MenuItemRecipes.Dtos;

public class UpsertMenuItemRecipeRequest
{
    public List<MenuItemRecipeLineInput> Lines { get; set; } = [];
}
