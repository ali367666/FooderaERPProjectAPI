using Domain.Common;

namespace Domain.Entities;

/// <summary>A table of a banquet order. A banquet holds several tables on one bill; every one of them, the order's main table included, has a row here.</summary>
public class OrderTable : CompanyEntity<int>
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public int TableId { get; set; }
    public RestaurantTable Table { get; set; } = default!;
}
