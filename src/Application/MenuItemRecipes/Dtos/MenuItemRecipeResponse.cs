namespace Application.MenuItemRecipes.Dtos;

public class MenuItemRecipeResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public List<MenuItemRecipeIngredientLineResponse> Lines { get; set; } = [];
}

public class MenuItemRecipeIngredientLineResponse
{
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
