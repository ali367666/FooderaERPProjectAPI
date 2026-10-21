namespace Application.Pos.Dtos;

public class PosSearchResponse
{
    /// <summary>Tables whose own details, or whose open order's details, match.</summary>
    public List<PosSearchTableResponse> Tables { get; set; } = new();

    /// <summary>Open orders that match — including take-away and delivery orders, which have no table on screen.</summary>
    public List<PosSearchOrderResponse> Orders { get; set; } = new();
}
