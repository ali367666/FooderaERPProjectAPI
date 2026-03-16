using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Warehouse : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public WarehouseType Type { get; set; }

    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public int? DriverUserId { get; set; }      // Vehicle anbarı sürücüyə bağlıdır
    public User? DriverUser { get; set; }       // istəməsən domain-a user bağlamaya bilərsən, sadəcə int saxla
}