namespace Application.PrinterStationType.Dtos;

public class UpdatePrinterStationTypeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
