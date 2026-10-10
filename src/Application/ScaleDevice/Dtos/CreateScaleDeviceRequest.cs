namespace Application.ScaleDevice.Dtos;

public class CreateScaleDeviceRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Brand { get; set; }
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}
