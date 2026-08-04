namespace Application.Analytics.Dtos;

public class FoodCostResponse
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public decimal SellingPrice { get; set; }
    public decimal FoodCost { get; set; }
    public decimal FoodCostPercentage { get; set; }   // FoodCost / SellingPrice * 100
    public decimal GrossProfit { get; set; }           // SellingPrice - FoodCost
    public decimal GrossProfitMargin { get; set; }     // GrossProfit / SellingPrice * 100
    public bool HasRecipe { get; set; }
    public bool HasMissingCost { get; set; }            // true if some ingredient has no purchase history
    public List<FoodCostLineDto> Lines { get; set; } = [];
}

public class FoodCostLineDto
{
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal QuantityPerPortion { get; set; }
    public string Unit { get; set; } = default!;
    public decimal UnitCost { get; set; }        // average AZN cost per unit from purchase history
    public decimal LineCost { get; set; }        // QuantityPerPortion * UnitCost
    public bool MissingCost { get; set; }
}
