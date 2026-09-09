using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class DeliveryIntegration : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public DeliveryProvider Provider { get; set; } = DeliveryProvider.Wolt;

    /// <summary>Platformun bu restoran üçün verdiyi mağaza/venue ID-si.</summary>
    public string? ExternalVenueId { get; set; }

    /// <summary>Platformdan alınan integrator ID / API açarı.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Webhook imzasını yoxlamaq üçün platformdan alınan HMAC sirri.</summary>
    public string? WebhookSecret { get; set; }

    public bool IsActive { get; set; } = true;
}
