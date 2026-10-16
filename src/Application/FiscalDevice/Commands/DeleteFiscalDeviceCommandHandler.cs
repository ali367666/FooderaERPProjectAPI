using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public class DeleteFiscalDeviceCommandHandler : IRequestHandler<DeleteFiscalDeviceCommand>
{
    private readonly IFiscalDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFiscalDeviceCommandHandler(IFiscalDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteFiscalDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var device = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (device is null)
            throw new Exception("Fiskal cihaz tapılmadı.");

        _repository.Delete(device);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
