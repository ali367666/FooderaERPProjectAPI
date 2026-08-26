namespace Application.PrinterStationType.Dtos;

public class PrinterStationTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class CreatePrinterStationTypeRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class UpdatePrinterStationTypeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
