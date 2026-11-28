namespace Application.Analytics.Dtos;

/// <summary>The cards of the food cost page. Averages count only dishes that have a recipe.</summary>
public class FoodCostSummaryResponse
{
    public decimal AverageFoodCostPercent { get; set; }
    public decimal AverageMarginPercent { get; set; }
    /// <summary>Dishes whose food cost is above 40 %.</summary>
    public int HighCostCount { get; set; }
    public int WithoutRecipeCount { get; set; }
    public int MissingCostCount { get; set; }
}
