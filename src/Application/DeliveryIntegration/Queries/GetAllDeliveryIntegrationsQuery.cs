using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.DeliveryIntegration.Commands;
using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Queries;

public record GetAllDeliveryIntegrationsQuery(int RestaurantId) : IRequest<List<DeliveryIntegrationResponse>>;

public class GetAllDeliveryIntegrationsQueryHandler : IRequestHandler<GetAllDeliveryIntegrationsQuery, List<DeliveryIntegrationResponse>>
{
    private readonly IDeliveryIntegrationRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllDeliveryIntegrationsQueryHandler(IDeliveryIntegrationRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<DeliveryIntegrationResponse>> Handle(GetAllDeliveryIntegrationsQuery request, CancellationToken cancellationToken)
    {
        var integrations = await _repository.GetAllByRestaurantAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        return integrations.Select(CreateDeliveryIntegrationCommandHandler.Map).ToList();
    }
}
