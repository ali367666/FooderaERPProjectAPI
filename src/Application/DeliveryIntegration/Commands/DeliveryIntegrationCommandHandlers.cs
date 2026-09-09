using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

public class CreateDeliveryIntegrationCommandHandler : IRequestHandler<CreateDeliveryIntegrationCommand, DeliveryIntegrationResponse>
{
    private readonly IDeliveryIntegrationRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDeliveryIntegrationCommandHandler(IDeliveryIntegrationRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<DeliveryIntegrationResponse> Handle(CreateDeliveryIntegrationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(dto.RestaurantId, name, null, cancellationToken))
            throw new Exception("Bu adda çatdırılma inteqrasiyası artıq mövcuddur.");

        var integration = new Domain.Entities.DeliveryIntegration
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            Provider = dto.Provider,
            ExternalVenueId = string.IsNullOrWhiteSpace(dto.ExternalVenueId) ? null : dto.ExternalVenueId.Trim(),
            ApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? null : dto.ApiKey.Trim(),
            WebhookSecret = string.IsNullOrWhiteSpace(dto.WebhookSecret) ? null : dto.WebhookSecret.Trim(),
            IsActive = dto.IsActive
        };

        await _repository.AddAsync(integration, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(integration);
    }

    internal static DeliveryIntegrationResponse Map(Domain.Entities.DeliveryIntegration d) => new()
    {
        Id = d.Id,
        RestaurantId = d.RestaurantId,
        Name = d.Name,
        Provider = d.Provider,
        ExternalVenueId = d.ExternalVenueId,
        ApiKey = d.ApiKey,
        WebhookSecret = d.WebhookSecret,
        IsActive = d.IsActive
    };
}

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

public class DeleteDeliveryIntegrationCommandHandler : IRequestHandler<DeleteDeliveryIntegrationCommand>
{
    private readonly IDeliveryIntegrationRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDeliveryIntegrationCommandHandler(IDeliveryIntegrationRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteDeliveryIntegrationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var integration = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (integration is null)
            throw new Exception("Çatdırılma inteqrasiyası tapılmadı.");

        _repository.Delete(integration);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
