using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

public class UpdateDeliveryIntegrationCommandHandler : IRequestHandler<UpdateDeliveryIntegrationCommand, DeliveryIntegrationResponse>
{
    private readonly IDeliveryIntegrationRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateDeliveryIntegrationCommandHandler(IDeliveryIntegrationRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<DeliveryIntegrationResponse> Handle(UpdateDeliveryIntegrationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var integration = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (integration is null)
            throw new Exception("Çatdırılma inteqrasiyası tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(integration.RestaurantId, name, integration.Id, cancellationToken))
            throw new Exception("Bu adda çatdırılma inteqrasiyası artıq mövcuddur.");

        integration.Name = name;
        integration.Provider = dto.Provider;
        integration.ExternalVenueId = string.IsNullOrWhiteSpace(dto.ExternalVenueId) ? null : dto.ExternalVenueId.Trim();
        integration.ApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? null : dto.ApiKey.Trim();
        integration.WebhookSecret = string.IsNullOrWhiteSpace(dto.WebhookSecret) ? null : dto.WebhookSecret.Trim();
        integration.IsActive = dto.IsActive;

        _repository.Update(integration);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateDeliveryIntegrationCommandHandler.Map(integration);
    }
}
