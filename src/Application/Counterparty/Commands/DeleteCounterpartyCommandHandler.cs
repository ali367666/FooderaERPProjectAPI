using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public class DeleteCounterpartyCommandHandler : IRequestHandler<DeleteCounterpartyCommand>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCounterpartyCommandHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCounterpartyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var counterparty = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (counterparty is null)
            throw new Exception("Konturagent tapılmadı.");

        _repository.Delete(counterparty);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
