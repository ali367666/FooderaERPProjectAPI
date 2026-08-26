using Domain.Common;

namespace Domain.Entities;

public class Printer : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int StationTypeId { get; set; }
    public PrinterStationType StationType { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; } = 9100;
    public bool IsActive { get; set; } = true;
}
