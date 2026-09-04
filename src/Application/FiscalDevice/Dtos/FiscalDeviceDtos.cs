using Domain.Enums;

namespace Application.FiscalDevice.Dtos;

public class FiscalDeviceResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public FiscalDeviceProvider Provider { get; set; }
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; }
}

public class CreateFiscalDeviceRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public FiscalDeviceProvider Provider { get; set; } = FiscalDeviceProvider.Other;
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateFiscalDeviceRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public FiscalDeviceProvider Provider { get; set; } = FiscalDeviceProvider.Other;
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}
