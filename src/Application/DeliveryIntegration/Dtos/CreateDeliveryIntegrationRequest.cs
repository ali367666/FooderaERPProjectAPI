using Domain.Enums;

namespace Application.DeliveryIntegration.Dtos;

public class CreateDeliveryIntegrationRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public DeliveryProvider Provider { get; set; } = DeliveryProvider.Wolt;
    public string? ExternalVenueId { get; set; }
    public string? ApiKey { get; set; }
    public string? WebhookSecret { get; set; }
    public bool IsActive { get; set; } = true;
}
