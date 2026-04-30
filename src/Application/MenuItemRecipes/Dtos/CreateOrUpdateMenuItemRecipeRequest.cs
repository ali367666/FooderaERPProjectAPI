namespace Application.MenuItemRecipes.Dtos;

public class CreateOrUpdateMenuItemRecipeRequest
{
    public int MenuItemId { get; set; }
    public List<MenuItemRecipeLineInput> Lines { get; set; } = [];
}
