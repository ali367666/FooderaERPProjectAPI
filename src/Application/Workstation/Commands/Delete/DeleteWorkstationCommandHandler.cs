using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Workstation.Commands.Delete;

public class DeleteWorkstationCommandHandler : IRequestHandler<DeleteWorkstationCommand, BaseResponse>
{
    private readonly IWorkstationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteWorkstationCommandHandler> _logger;

    public DeleteWorkstationCommandHandler(
        IWorkstationRepository repository,
        ICurrentUserService currentUserService,
        ILogger<DeleteWorkstationCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(DeleteWorkstationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var workstation = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (workstation is null)
            return BaseResponse.Fail("Terminal tapılmadı.");

        _repository.Delete(workstation);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("POS workstation deleted. Id: {WorkstationId}, CompanyId: {CompanyId}", request.Id, companyId);

        return BaseResponse.Ok("Terminal silindi.");
    }
}
