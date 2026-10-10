namespace Application.Analytics.Dtos;

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
