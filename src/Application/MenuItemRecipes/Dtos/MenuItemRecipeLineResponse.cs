namespace Application.MenuItemRecipes.Dtos;

public class MenuItemRecipeLineResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal QuantityPerPortion { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string StockItemUnit { get; set; } = string.Empty;
}
