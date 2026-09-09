using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DeliveryIntegrationRepository : IDeliveryIntegrationRepository
{
    private readonly AppDbContext _context;

    public DeliveryIntegrationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DeliveryIntegration integration, CancellationToken cancellationToken)
    {
        await _context.DeliveryIntegrations.AddAsync(integration, cancellationToken);
    }

    public async Task<DeliveryIntegration?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.DeliveryIntegrations
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<DeliveryIntegration>> GetAllByRestaurantAsync(int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        return await _context.DeliveryIntegrations
            .Where(x => x.CompanyId == companyId && x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliveryIntegration?> GetByIdForWebhookAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.DeliveryIntegrations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DeliveryIntegration?> GetActiveByProviderAndVenueAsync(DeliveryProvider provider, string externalVenueId, CancellationToken cancellationToken)
    {
        return await _context.DeliveryIntegrations
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ExternalVenueId == externalVenueId && x.IsActive,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int restaurantId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        return await _context.DeliveryIntegrations
            .AnyAsync(x => x.RestaurantId == restaurantId && x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public void Update(DeliveryIntegration integration) => _context.DeliveryIntegrations.Update(integration);
    public void Delete(DeliveryIntegration integration) => _context.DeliveryIntegrations.Remove(integration);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
