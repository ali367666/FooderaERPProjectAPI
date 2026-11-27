namespace Application.Banquets.Dtos;

/// <summary>Opens a banquet order on the chosen tables right now (one bill for all of them).</summary>
public class OpenBanquetRequest
{
    public int RestaurantId { get; set; }
    public int WaiterId { get; set; }

    /// <summary>The tables picked in the hall; the first one becomes the order's main table.</summary>
    public List<int> TableIds { get; set; } = new();

    public int? GuestCount { get; set; }
    public string? Note { get; set; }
}
