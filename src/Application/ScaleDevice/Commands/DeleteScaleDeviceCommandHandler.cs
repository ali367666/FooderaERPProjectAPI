using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Commands;

public class DeleteScaleDeviceCommandHandler : IRequestHandler<DeleteScaleDeviceCommand>
{
    private readonly IScaleDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteScaleDeviceCommandHandler(IScaleDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteScaleDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var device = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (device is null)
            throw new Exception("Tərəzi tapılmadı.");

        _repository.Delete(device);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
