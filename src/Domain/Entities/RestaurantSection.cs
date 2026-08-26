using Domain.Common;

namespace Domain.Entities;

public class RestaurantSection : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
