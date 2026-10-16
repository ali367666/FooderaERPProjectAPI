namespace Application.PrinterStationType.Dtos;

public class CreatePrinterStationTypeRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
