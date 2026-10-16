using Domain.Enums;

namespace Application.FiscalDevice.Dtos;

public class UpdateFiscalDeviceRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public FiscalDeviceProvider Provider { get; set; } = FiscalDeviceProvider.Other;
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}
