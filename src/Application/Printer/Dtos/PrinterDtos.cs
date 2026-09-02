namespace Application.Printer.Dtos;

public class PrinterResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int StationTypeId { get; set; }
    public string StationTypeName { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; }
    public bool IsActive { get; set; }
    public bool IsPrimary { get; set; }
}

public class CreatePrinterRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int StationTypeId { get; set; }
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; } = 9100;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
}

public class UpdatePrinterRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int StationTypeId { get; set; }
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; } = 9100;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
}

public class PrintRequest
{
    public string Content { get; set; } = default!;
}
