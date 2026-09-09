using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IDeliveryIntegrationRepository
{
    Task AddAsync(Domain.Entities.DeliveryIntegration integration, CancellationToken cancellationToken);
    Task<Domain.Entities.DeliveryIntegration?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);

    /// <summary>Company-unscoped lookup for inbound webhook calls, which arrive with no JWT/company context.
    /// The integration's own WebhookSecret is what actually authenticates the caller — this ID is not a secret.</summary>
    Task<Domain.Entities.DeliveryIntegration?> GetByIdForWebhookAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.DeliveryIntegration>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken);
    Task<Domain.Entities.DeliveryIntegration?> GetActiveByProviderAndVenueAsync(DeliveryProvider provider, string externalVenueId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.DeliveryIntegration integration);
    void Delete(Domain.Entities.DeliveryIntegration integration);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
