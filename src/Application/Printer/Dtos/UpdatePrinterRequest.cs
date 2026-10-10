namespace Application.Printer.Dtos;

public class UpdatePrinterRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int StationTypeId { get; set; }
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; } = 9100;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public bool IsChiefPrinter { get; set; }
}
