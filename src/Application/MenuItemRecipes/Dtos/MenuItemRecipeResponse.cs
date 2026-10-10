namespace Application.MenuItemRecipes.Dtos;

public class MenuItemRecipeResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public List<MenuItemRecipeIngredientLineResponse> Lines { get; set; } = [];
}
