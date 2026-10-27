using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class RestaurantSection : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public RestaurantTableType Type { get; set; } = RestaurantTableType.Masa;

    /// <summary>Banquet hall: every order opened on one of its tables is a banquet order (banquet menu, banquet prices).</summary>
    public bool IsBanquet { get; set; }

    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
