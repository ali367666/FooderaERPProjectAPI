using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

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
