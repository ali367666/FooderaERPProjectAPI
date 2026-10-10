namespace Application.MenuItemRecipes.Dtos;

public class MenuItemRecipeIngredientLineResponse
{
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
