using Domain.Enums;

namespace Application.MenuItemRecipes.Dtos;

public class MenuItemRecipeLineInput
{
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityPerPortion { get; set; }
    public UnitOfMeasure? Unit { get; set; }
}
