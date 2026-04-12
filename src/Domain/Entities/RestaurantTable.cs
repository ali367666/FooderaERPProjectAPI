using Domain.Common;

namespace Domain.Entities;

public class RestaurantTable : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsOccupied { get; set; } = false;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}